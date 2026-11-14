using System.Runtime.InteropServices;
using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    /// <summary>
    /// 广成科技 USBCANFD 封装(ECANFDVCI64.dll / ECanFDVci API)。
    /// 通道按 CANFD 初始化,同一通道可同时收发 CANFD 帧与经典 CAN 帧;
    /// 由 <see cref="GuangChengCanFDConfig.UseCanFD"/> 决定是否允许 FD 帧。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.GuangCheng, GuangChengDeviceType.USBCANFD, CommProtocol.CANFD)]
    [HardwareDefAttributes(HardwareManufacturer.GuangCheng, GuangChengDeviceType.USBCANFD, CommProtocol.CAN)]
    public class GuangChengCanFdWrapper : IHardware
    {
        private readonly GuangChengCanFDConfig _canfdConfig = new GuangChengCanFDConfig();

        //Open 时的设备参数/模式快照,会话期间固定使用
        private uint _openedDeviceIndex = 0;
        private byte _openedChannel = 0;
        private bool _openedUseCanFD = true;

        //接收缓冲区在 Open 时一次性分配,Close 时释放
        private const int RxBatchSize = 100;    //单次 P/Invoke 最多读取的帧数
        private IntPtr _rxBuffer = IntPtr.Zero;
        private int _rxFrameSize = 0;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _canfdConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();

        // Task控制相关
        private bool _isOpen = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        public bool Close()
        {
            lock (_lock)
            {
                if (!_isOpen) return true;
                if (_cts != null) _cts.Cancel();
            }

            // 在锁外部等待Task完成，避免死锁
            if (_readTask != null)
            {
                try { _readTask.Wait(1000); }
                catch (AggregateException)
                {
                    // 忽略Task取消时的异常
                }
            }

            lock (_lock)
            {
                if (_cts != null) { _cts.Dispose(); _cts = null; }
                _readTask = null;

                if (_isOpen)
                {
                    try { GuangChengCanFdLibDLL.StopCANFD(GuangChengCanFdLibDLL.USBCANFD, _openedDeviceIndex, _openedChannel); } catch { }
                    try { GuangChengCanFdLibDLL.CloseDeviceFD(GuangChengCanFdLibDLL.USBCANFD, _openedDeviceIndex); } catch { }
                    FreeBuffers();
                    _isOpen = false;
                }
            }
            return true;
        }

        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (!_isOpen || _rxBuffer == IntPtr.Zero) return;

                // ECanFDVci 没有独立的清缓冲接口,循环读取直到队列为空
                for (int i = 0; i < 1000; i++)
                {
                    uint len = RxBatchSize;
                    uint status = GuangChengCanFdLibDLL.ReceiveFD(
                        GuangChengCanFdLibDLL.USBCANFD, _openedDeviceIndex, _openedChannel, _rxBuffer, ref len);
                    if (status != GuangChengCanFdLibDLL.STATUS_OK || len == 0) break;
                }
            }
        }

        public void FlushTransmitBuffer()
        {
            // 被动发送模式的发送缓冲由驱动管理,接口库没有清空发送缓冲的函数
        }

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                uint deviceType = GuangChengCanFdLibDLL.USBCANFD;
                uint deviceIndex = _canfdConfig.DeviceIndex;
                byte channel = (byte)_canfdConfig.DeviceChannel;
                bool useCanFD = _canfdConfig.UseCanFD;
                IntPtr rxBuffer = IntPtr.Zero;
                bool deviceOpened = false;

                try
                {
                    // 打开设备(同一物理设备的两个通道共享一次 Open)
                    if (!GuangChengCanFdLibDLL.OpenDeviceFD(deviceType, deviceIndex))
                        throw new Exception("Open device failed");
                    deviceOpened = true;

                    GuangChengCanFdLibDLL.INIT_CONFIG initConfig = BuildInitConfig();

                    if (GuangChengCanFdLibDLL.InitCANFD(deviceType, deviceIndex, channel, ref initConfig) != GuangChengCanFdLibDLL.STATUS_OK)
                        throw new Exception("Initialize can channel failed");

                    // 分配持久化接收缓冲区
                    _rxFrameSize = Marshal.SizeOf<GuangChengCanFdLibDLL.CANFD_OBJ>();
                    rxBuffer = Marshal.AllocHGlobal(_rxFrameSize * RxBatchSize);

                    if (GuangChengCanFdLibDLL.StartCANFD(deviceType, deviceIndex, channel) != GuangChengCanFdLibDLL.STATUS_OK)
                        throw new Exception("Start can channel failed");

                    // 厂商手册要求在 StartCANFD 后延迟 10ms 再执行 TransmitFD
                    Thread.Sleep(10);

                    // 所有操作成功后，才提交状态
                    _openedDeviceIndex = deviceIndex;
                    _openedChannel = channel;
                    _openedUseCanFD = useCanFD;
                    _rxBuffer = rxBuffer;
                    rxBuffer = IntPtr.Zero;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (rxBuffer != IntPtr.Zero) Marshal.FreeHGlobal(rxBuffer);
                    FreeBuffers();
                    try { GuangChengCanFdLibDLL.ResetCANFD(deviceType, deviceIndex, channel); } catch { }
                    if (deviceOpened) { try { GuangChengCanFdLibDLL.CloseDeviceFD(deviceType, deviceIndex); } catch { } }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _isOpen = false;
                    _rxFrameSize = 0;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        private GuangChengCanFdLibDLL.INIT_CONFIG BuildInitConfig()
        {
            GuangChengCanFdLibDLL.INIT_CONFIG initConfig = new GuangChengCanFdLibDLL.INIT_CONFIG
            {
                CanReceMode = (byte)_canfdConfig.ReceiveMode,
                CanSendMode = (byte)_canfdConfig.SendMode,
                // NominalBitRate/DataBitRate 由驱动根据 *Select 字段计算,调用方保持 0
                NominalBitRate = 0,
                DataBitRate = 0,
                NominalBitRateSelect = (byte)_canfdConfig.NominalBitrate,
                DataBitRateSelect = (byte)_canfdConfig.DataBitrate,
            };

            if (_canfdConfig.EnableFilter)
            {
                // 使用滤波器1:FilterUsedBits bit0=1,StdOrExdBits bit0 决定标准/扩展
                initConfig.CanReceMode = (byte)GuangChengCanFdReceiveMode.Specified;
                initConfig.FilterUsedBits = 0x01;
                initConfig.StdOrExdBits = (byte)(_canfdConfig.FilterExtended ? 0x01 : 0x00);
                initConfig.StandardORExtendedfilter1 = _canfdConfig.FilterId;
                initConfig.StandardORExtendedfilter1Mask = _canfdConfig.FilterMask;
            }
            else if (_canfdConfig.ReceiveMode == GuangChengCanFdReceiveMode.Specified)
            {
                throw new InvalidOperationException(
                    "ReceiveMode=Specified requires EnableFilter=true and a configured filter.");
            }

            return initConfig;
        }

        private void FreeBuffers()
        {
            if (_rxBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(_rxBuffer); _rxBuffer = IntPtr.Zero; }
            _rxFrameSize = 0;
        }

        private async Task ReadTaskProc(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    bool res;
                    // 连续读取直到无数据
                    do
                    {
                        if (token.IsCancellationRequested) break;
                        res = Read();
                    }
                    while (res && !token.IsCancellationRequested);

                    if (!token.IsCancellationRequested) await Task.Delay(5, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    try { if (!token.IsCancellationRequested) await Task.Delay(10, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }

        private bool Read()
        {
            lock (_lock)
            {
                if (!_isOpen || _rxBuffer == IntPtr.Zero) return false;

                try
                {
                    uint len = RxBatchSize;
                    uint status = GuangChengCanFdLibDLL.ReceiveFD(
                        GuangChengCanFdLibDLL.USBCANFD, _openedDeviceIndex, _openedChannel, _rxBuffer, ref len);

                    if (status != GuangChengCanFdLibDLL.STATUS_OK || len == 0) return false;

                    int count = (int)Math.Min(len, (uint)RxBatchSize);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr pFrame = IntPtr.Add(_rxBuffer, i * _rxFrameSize);
                        var recData = Marshal.PtrToStructure<GuangChengCanFdLibDLL.CANFD_OBJ>(pFrame);

                        CANFrame? frame = ParseFrame(recData);
                        if (frame != null) eventHandler?.Invoke(this, frame);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Read)}:{ex.Message}");
                }
            }
        }

        private CANFrame? ParseFrame(GuangChengCanFdLibDLL.CANFD_OBJ recData)
        {
            byte rawType = recData.CanORCanfdType;
            bool isFd = (rawType & GuangChengCanFdFlagParser.PROTO_CANFD) != 0;

            // 经典 CAN 模式不向上层推送 FD 帧
            if (isFd && !_openedUseCanFD) return null;

            bool isExtended = (rawType & GuangChengCanFdFlagParser.FORMAT_EXTENDED) != 0;
            bool isRtr = (rawType & GuangChengCanFdFlagParser.TYPE_RTR) != 0;
            bool brs = (rawType & GuangChengCanFdFlagParser.BITRATE_SWITCH) != 0;

            // 转换为工程约定的 SocketCAN 风格 ID
            long id = recData.ID & CanFrameDef.CAN_ID_FLAG;
            if (isExtended) id |= CanFrameDef.CAN_EFF_FLAG;
            if (isRtr) id |= CanFrameDef.CAN_RTR_FLAG;

            int dataLen = Math.Min((int)recData.DataLen,
                isFd ? DataLenConverter.MaxCanFdDataLength : DataLenConverter.MaxCanDataLength);

            CANFrame frame = new CANFrame
            {
                ID = id,
                DLC = dataLen,
                Flag = (int)recData.ID,
                Flags = GuangChengCanFdFlagParser.Parse(rawType),
                Timestamp = ComposeTimestamp(recData.TimeStamp),
                FDF = isFd,
                BRS = isFd && brs,
                Data = new byte[dataLen],
                Dir = 1,
            };

            if (dataLen > 0 && recData.Data != null)
                Array.Copy(recData.Data, 0, frame.Data, 0, dataLen);

            return frame;
        }

        /// <summary>
        /// 将厂商的绝对时间戳(月内的日/时/分/秒/毫秒/微秒)换算为微秒计数。
        /// 由于结构体没有年月字段,该值适合表示同一天内的时间顺序,不能当作 Unix 时间。
        /// </summary>
        private static ulong ComposeTimestamp(GuangChengCanFdLibDLL.TIMESTAMP_TYPE ts)
        {
            ulong seconds = (ulong)ts.mday * 86400UL
                          + (ulong)ts.hour * 3600UL
                          + (ulong)ts.minute * 60UL
                          + ts.second;
            return seconds * 1_000_000UL
                 + (ulong)ts.millisecond * 1000UL
                 + ts.microsecond;
        }

        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen) return false;
                if (frame is not CANFrame canFrame) return false;

                try
                {
                    return SendCore(canFrame);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }

        private bool SendCore(CANFrame canFrame)
        {
            if (canFrame.BRS && !canFrame.FDF)
                throw new ArgumentException("BRS is only valid for CANFD frame(FDF must be set).");
            if (canFrame.FDF && !_openedUseCanFD)
                throw new ArgumentException("CANFD frame(FDF) is disabled by UseCanFD=false.");

            bool validLength = canFrame.FDF
                ? DataLenConverter.IsValidCanFdLength(canFrame.DLC)
                : DataLenConverter.IsValidCanLength(canFrame.DLC);
            if (!validLength)
                throw new ArgumentException(canFrame.FDF
                    ? $"Invalid CANFD DLC({canFrame.DLC}), valid lengths: 0-8,12,16,20,24,32,48,64."
                    : $"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");

            bool isRtr = CanFrameHelper.IsRTR(canFrame.ID);
            if (canFrame.FDF && isRtr)
                throw new ArgumentException("CANFD does not support remote frame(RTR).");
            if (!isRtr && (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC))
                throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

            canFrame.Dir = 0;
            uint validId = CanFrameHelper.GetId(canFrame.ID);
            bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);

            byte rawType = GuangChengCanFdLibDLL.BuildFrameType(canFrame.FDF, isExtended, isRtr, canFrame.BRS);

            GuangChengCanFdLibDLL.CANFD_OBJ sendObj = new GuangChengCanFdLibDLL.CANFD_OBJ
            {
                CanORCanfdType = rawType,
                DataLen = (byte)canFrame.DLC,
                Reserved = new byte[2],
                ID = validId,
                TimeStamp = new GuangChengCanFdLibDLL.TIMESTAMP_TYPE(),
                Data = new byte[DataLenConverter.MaxCanFdDataLength],
            };

            if (!isRtr && canFrame.DLC > 0)
                Array.Copy(canFrame.Data, 0, sendObj.Data, 0, canFrame.DLC);

            uint result = GuangChengCanFdLibDLL.TransmitFD(
                GuangChengCanFdLibDLL.USBCANFD, _openedDeviceIndex, _openedChannel, ref sendObj, 1);
            if (result != GuangChengCanFdLibDLL.STATUS_OK) return false;

            canFrame.Flags = GuangChengCanFdFlagParser.Parse(rawType);
            eventHandler?.Invoke(this, canFrame);
            return true;
        }
    }
}
