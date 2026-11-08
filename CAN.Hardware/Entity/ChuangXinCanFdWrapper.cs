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
    /// 创芯科技 USBCANFD 分析仪封装(ControlCANFD.dll / ZCAN_ API)。
    /// 适用型号:USBCANFD-200U、USBCANFD-100U、USBCANFD-MINI。
    /// 同时支持 CANFD 与经典 CAN,由 <see cref="ChuangXinCanFDConfig.UseCanFD"/> 决定是否收发 FD 帧。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.ChuangXin, ChuangXinDeviceType.USBCANFD, CommProtocol.CANFD)]
    [HardwareDefAttributes(HardwareManufacturer.ChuangXin, ChuangXinDeviceType.USBCANFD, CommProtocol.CAN)]
    public class ChuangXinCanFdWrapper : IHardware
    {
        private readonly ChuangXinCanFDConfig _canfdConfig = new ChuangXinCanFDConfig();

        //Open 时的设备参数/模式快照,会话期间固定使用
        private uint _openedDeviceType = 0;
        private uint _openedDeviceIndex = 0;
        private uint _openedChannel = 0;
        private IntPtr _openedChannelHandle = IntPtr.Zero;
        private bool _openedUseCanFD = true;

        private const int RxBatchSize = 100;    //单次 P/Invoke 最多读取的帧数
        private IntPtr _rxCanBuffer = IntPtr.Zero;
        private int _rxCanFrameSize = 0;
        private IntPtr _rxFdBuffer = IntPtr.Zero;
        private int _rxFdFrameSize = 0;
        private IntPtr _txBuffer = IntPtr.Zero;

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
                    try { ChuangXinCanFdLibDLL.ZCAN_ResetCAN(_openedChannelHandle); } catch { }
                    try { ChuangXinCanFdLibDLL.CloseDevice(_openedDeviceType, _openedDeviceIndex); } catch { }
                    _openedChannelHandle = IntPtr.Zero;
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
                if (_isOpen) ChuangXinCanFdLibDLL.ZCAN_ClearBuffer(_openedChannelHandle);
            }
        }

        public void FlushTransmitBuffer()
        {
            lock (_lock)
            {
                if (_isOpen) ChuangXinCanFdLibDLL.ZCAN_ClearBuffer(_openedChannelHandle);
            }
        }

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                uint deviceType = (uint)_canfdConfig.DeviceModel;
                uint deviceIndex = _canfdConfig.DeviceIndex;
                uint channel = _canfdConfig.DeviceChannel;
                bool useCanFD = _canfdConfig.UseCanFD;

                IntPtr devHandle = IntPtr.Zero;
                IntPtr chHandle = IntPtr.Zero;
                IntPtr rxCanBuffer = IntPtr.Zero;
                IntPtr rxFdBuffer = IntPtr.Zero;
                IntPtr txBuffer = IntPtr.Zero;

                try
                {
                    // 打开设备(同一物理设备的两个通道共享一个设备句柄)
                    devHandle = ChuangXinCanFdLibDLL.OpenDevice(deviceType, deviceIndex);
                    if (devHandle == IntPtr.Zero) throw new Exception("Open device failed");

                    // CANFD 标准(ISO/BOSCH)
                    uint status = ChuangXinCanFdLibDLL.ZCAN_SetCANFDStandard(devHandle, channel, (uint)_canfdConfig.CANFDStandard);
                    if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Set canfd standard failed({status})");

                    // 波特率(必须在 InitCAN 之前设置)
                    if (_canfdConfig.EnableCustomBitrate)
                    {
                        if (string.IsNullOrWhiteSpace(_canfdConfig.CustomBitrateContent))
                            throw new ArgumentException("CustomBitrateContent is empty.");
                        status = ChuangXinCanFdLibDLL.ZCAN_SetBaudRateCustom(devHandle, channel, _canfdConfig.CustomBitrateContent);
                        if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Set custom bitrate failed({status})");
                    }
                    else
                    {
                        status = ChuangXinCanFdLibDLL.ZCAN_SetAbitBaud(devHandle, channel,
                            ChuangXinCanFdLibDLL.BuildCanBitrate(_canfdConfig.ArbitrationBitrate));
                        if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Set arbitration bitrate failed({status})");

                        //通道始终按 CANFD 类型初始化,数据段波特率一并设置
                        status = ChuangXinCanFdLibDLL.ZCAN_SetDbitBaud(devHandle, channel,
                            ChuangXinCanFdLibDLL.BuildCanFdBitrate(_canfdConfig.DataBitrate));
                        if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Set data bitrate failed({status})");
                    }

                    // 120Ω 终端电阻(官方样例在 InitCAN 之前设置)。
                    // 关闭电阻时若设备不支持该属性则忽略;显式开启失败才视为错误。
                    status = ChuangXinCanFdLibDLL.ZCAN_SetResistanceEnable(devHandle, channel,
                        (uint)(_canfdConfig.EnableA120Ohm ? 1 : 0));
                    if (status != ChuangXinCanFdLibDLL.STATUS_OK && _canfdConfig.EnableA120Ohm)
                        throw new Exception($"Set internal resistance failed({status})");

                    // 初始化通道:USBCANFD 设备统一以 CANFD 类型初始化(官方样例做法),
                    // 经典 CAN 帧仍可通过 ZCAN_Transmit/ZCAN_Receive 收发。
                    ChuangXinCanFdLibDLL.ZCAN_CHANNEL_INIT_CONFIG initConfig = new ChuangXinCanFdLibDLL.ZCAN_CHANNEL_INIT_CONFIG
                    {
                        can_type = ChuangXinCanFdLibDLL.TYPE_CANFD,
                    };
                    initConfig.canfd.acc_code = 0;
                    initConfig.canfd.acc_mask = 0xFFFFFFFF;
                    initConfig.canfd.abit_timing = 0;
                    initConfig.canfd.dbit_timing = 0;
                    initConfig.canfd.brp = 0;
                    initConfig.canfd.filter = 0;
                    initConfig.canfd.mode = (byte)_canfdConfig.Mode;
                    initConfig.canfd.pad = 0;
                    initConfig.canfd.reserved = 0;

                    IntPtr pInitConfig = Marshal.AllocHGlobal(Marshal.SizeOf<ChuangXinCanFdLibDLL.ZCAN_CHANNEL_INIT_CONFIG>());
                    try
                    {
                        Marshal.StructureToPtr(initConfig, pInitConfig, true);
                        chHandle = ChuangXinCanFdLibDLL.ZCAN_InitCAN(devHandle, channel, pInitConfig);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pInitConfig);
                    }
                    if (chHandle == IntPtr.Zero) throw new Exception("Initialize can channel failed");

                    // 过滤器(通道句柄,InitCAN 之后设置)
                    if (_canfdConfig.EnableFilter)
                    {
                        status = ChuangXinCanFdLibDLL.ZCAN_ClearFilter(chHandle);
                        if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Clear filter failed({status})");
                        status = ChuangXinCanFdLibDLL.ZCAN_SetFilterMode(chHandle, (uint)(_canfdConfig.ExtendedFilter ? 1 : 0));
                        if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Set filter mode failed({status})");
                        status = ChuangXinCanFdLibDLL.ZCAN_SetFilterStartID(chHandle, _canfdConfig.Filter_FromId);
                        if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Set filter start id failed({status})");
                        status = ChuangXinCanFdLibDLL.ZCAN_SetFilterEndID(chHandle, _canfdConfig.Filter_ToId);
                        if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Set filter end id failed({status})");
                        status = ChuangXinCanFdLibDLL.ZCAN_AckFilter(chHandle);
                        if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Ack filter failed({status})");
                    }
                    else
                    {
                        // 未启用过滤:只清除已有滤波配置(best-effort;InitCAN 后默认接收所有帧)。
                        // 手册中 filter_mode 仅定义 0=标准帧、1=扩展帧,没有"禁能"模式。
                        ChuangXinCanFdLibDLL.ZCAN_ClearFilter(chHandle);
                    }

                    // 分配持久化收发缓冲区
                    _rxCanFrameSize = Marshal.SizeOf<ChuangXinCanFdLibDLL.ZCAN_Receive_Data>();
                    rxCanBuffer = Marshal.AllocHGlobal(_rxCanFrameSize * RxBatchSize);
                    _rxFdFrameSize = Marshal.SizeOf<ChuangXinCanFdLibDLL.ZCAN_ReceiveFD_Data>();
                    rxFdBuffer = Marshal.AllocHGlobal(_rxFdFrameSize * RxBatchSize);
                    int txSize = Math.Max(
                        Marshal.SizeOf<ChuangXinCanFdLibDLL.ZCAN_Transmit_Data>(),
                        Marshal.SizeOf<ChuangXinCanFdLibDLL.ZCAN_TransmitFD_Data>());
                    txBuffer = Marshal.AllocHGlobal(txSize);

                    // 启动通道
                    status = ChuangXinCanFdLibDLL.ZCAN_StartCAN(chHandle);
                    if (status != ChuangXinCanFdLibDLL.STATUS_OK) throw new Exception($"Start can channel failed({status})");

                    // 所有操作成功后，才提交状态
                    _openedDeviceType = deviceType;
                    _openedDeviceIndex = deviceIndex;
                    _openedChannel = channel;
                    _openedChannelHandle = chHandle;
                    _openedUseCanFD = useCanFD;
                    _rxCanBuffer = rxCanBuffer;
                    _rxFdBuffer = rxFdBuffer;
                    _txBuffer = txBuffer;
                    rxCanBuffer = IntPtr.Zero;
                    rxFdBuffer = IntPtr.Zero;
                    txBuffer = IntPtr.Zero;

                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (rxCanBuffer != IntPtr.Zero) Marshal.FreeHGlobal(rxCanBuffer);
                    if (rxFdBuffer != IntPtr.Zero) Marshal.FreeHGlobal(rxFdBuffer);
                    if (txBuffer != IntPtr.Zero) Marshal.FreeHGlobal(txBuffer);
                    FreeBuffers();
                    if (chHandle != IntPtr.Zero) { try { ChuangXinCanFdLibDLL.ZCAN_ResetCAN(chHandle); } catch { } }
                    if (devHandle != IntPtr.Zero) { try { ChuangXinCanFdLibDLL.CloseDevice(deviceType, deviceIndex); } catch { } }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _isOpen = false;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        private void FreeBuffers()
        {
            if (_rxCanBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(_rxCanBuffer); _rxCanBuffer = IntPtr.Zero; }
            if (_rxFdBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(_rxFdBuffer); _rxFdBuffer = IntPtr.Zero; }
            if (_txBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(_txBuffer); _txBuffer = IntPtr.Zero; }
            _rxCanFrameSize = 0;
            _rxFdFrameSize = 0;
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
                if (!_isOpen) return false;

                try
                {
                    bool hasData = false;

                    // 经典 CAN 队列(非阻塞批量读取)
                    if (_rxCanBuffer != IntPtr.Zero)
                    {
                        uint receiveNum = ChuangXinCanFdLibDLL.ZCAN_Receive(_openedChannelHandle, _rxCanBuffer, RxBatchSize, 0);
                        // 0xFFFFFFFF(-1) 表示设备不存在/USB 掉线,按无数据处理
                        if (receiveNum != 0xFFFFFFFF)
                        {
                            if (receiveNum > 0) hasData = true;
                            int count = (int)Math.Min(receiveNum, (uint)RxBatchSize);
                            for (int i = 0; i < count; i++)
                            {
                                IntPtr pFrame = IntPtr.Add(_rxCanBuffer, i * _rxCanFrameSize);
                                CANFrame? frame = ParseCanFrame(pFrame);
                                if (frame != null) eventHandler?.Invoke(this, frame);
                            }
                        }
                    }

                    // CAN FD 队列(仅 FD 模式读取)
                    if (_openedUseCanFD && _rxFdBuffer != IntPtr.Zero)
                    {
                        uint receiveNum = ChuangXinCanFdLibDLL.ZCAN_ReceiveFD(_openedChannelHandle, _rxFdBuffer, RxBatchSize, 0);
                        // 0xFFFFFFFF(-1) 表示设备不存在/USB 掉线,按无数据处理
                        if (receiveNum != 0xFFFFFFFF)
                        {
                            if (receiveNum > 0) hasData = true;
                            int count = (int)Math.Min(receiveNum, (uint)RxBatchSize);
                            for (int i = 0; i < count; i++)
                            {
                                IntPtr pFrame = IntPtr.Add(_rxFdBuffer, i * _rxFdFrameSize);
                                CANFrame? frame = ParseFdFrame(pFrame);
                                if (frame != null) eventHandler?.Invoke(this, frame);
                            }
                        }
                    }

                    return hasData;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Read)}:{ex.Message}");
                }
            }
        }

        private static CANFrame? ParseCanFrame(IntPtr pFrame)
        {
            var recData = Marshal.PtrToStructure<ChuangXinCanFdLibDLL.ZCAN_Receive_Data>(pFrame);
            if (recData.frame.data == null) return null;

            CANFrame frame = new CANFrame();
            //ZCAN 的 can_id 高位自带 SocketCAN 风格 EFF/RTR/ERR 标志,直接透传
            frame.ID = recData.frame.can_id;
            //钳制 dlc 到经典 CAN 有效范围，防止驱动返回异常值导致越界
            frame.DLC = Math.Min((int)recData.frame.can_dlc, DataLenConverter.MaxCanDataLength);
            frame.Flag = (int)recData.frame.can_id;
            frame.Flags = ChuangXinCanFdFlagParser.Parse(recData.frame.can_id, 0, false);
            //ControlCANFD 时间戳单位为微秒
            frame.Timestamp = recData.timestamp;
            frame.Data = new byte[frame.DLC];
            frame.Dir = 1;
            if (frame.DLC > 0) Array.Copy(recData.frame.data, 0, frame.Data, 0, frame.DLC);
            return frame;
        }

        private static CANFrame? ParseFdFrame(IntPtr pFrame)
        {
            var recData = Marshal.PtrToStructure<ChuangXinCanFdLibDLL.ZCAN_ReceiveFD_Data>(pFrame);
            if (recData.frame.data == null) return null;

            CANFrame frame = new CANFrame();
            frame.ID = recData.frame.can_id;
            //CANFD 的 len 直接是实际字节数(0-64),无需 DLC 换算;钳制防止驱动返回异常值
            frame.DLC = Math.Min((int)recData.frame.len, DataLenConverter.MaxCanFdDataLength);
            frame.Flag = (int)recData.frame.can_id;
            frame.Flags = ChuangXinCanFdFlagParser.Parse(recData.frame.can_id, recData.frame.flags, true);
            //ControlCANFD 时间戳单位为微秒
            frame.Timestamp = recData.timestamp;
            //收到 CANFD 队列的帧即为 FD 帧;BRS/ESI 从 flags 解析
            frame.FDF = true;
            frame.BRS = (recData.frame.flags & 0x01) != 0;
            frame.Data = new byte[frame.DLC];
            frame.Dir = 1;
            if (frame.DLC > 0) Array.Copy(recData.frame.data, 0, frame.Data, 0, frame.DLC);
            return frame;
        }

        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _txBuffer == IntPtr.Zero) return false;
                if (frame is not CANFrame canFrame) return false;

                try
                {
                    return canFrame.FDF ? SendFd(canFrame) : SendCan(canFrame);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }

        private bool SendCan(CANFrame canFrame)
        {
            if (canFrame.BRS)
                throw new ArgumentException("BRS is only valid for CANFD frame(FDF must be set).");
            if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
                throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");

            bool isRTR = CanFrameHelper.IsRTR(canFrame.ID);
            if (!isRTR && (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC))
                throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

            canFrame.Dir = 0;
            uint validId = CanFrameHelper.GetId(canFrame.ID);
            bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);

            ChuangXinCanFdLibDLL.ZCAN_Transmit_Data sendData = new ChuangXinCanFdLibDLL.ZCAN_Transmit_Data();
            sendData.frame = new ChuangXinCanFdLibDLL.can_frame
            {
                can_id = ChuangXinCanFdLibDLL.MakeCanId(validId, isExtended, isRTR, false),
                can_dlc = (byte)canFrame.DLC,
                data = new byte[DataLenConverter.MaxCanDataLength],
            };
            if (!isRTR && canFrame.DLC > 0)
                Array.Copy(canFrame.Data, 0, sendData.frame.data, 0, canFrame.DLC);
            sendData.transmit_type = 0;

            Marshal.StructureToPtr(sendData, _txBuffer, true);
            uint result = ChuangXinCanFdLibDLL.ZCAN_Transmit(_openedChannelHandle, _txBuffer, 1);
            if (result != 1) return false;

            canFrame.Flags = ChuangXinCanFdFlagParser.Parse(sendData.frame.can_id, 0, false);
            eventHandler?.Invoke(this, canFrame);
            return true;
        }

        private bool SendFd(CANFrame canFrame)
        {
            if (!_openedUseCanFD)
                throw new ArgumentException("Classic CAN mode does not support CANFD frame(FDF).");
            if (canFrame.BRS && !canFrame.FDF)
                throw new ArgumentException("BRS is only valid for CANFD frame(FDF must be set).");
            if (!DataLenConverter.IsValidCanFdLength(canFrame.DLC))
                throw new ArgumentException($"Invalid CANFD DLC({canFrame.DLC}), valid lengths: 0-8,12,16,20,24,32,48,64.");
            if (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC)
                throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

            canFrame.Dir = 0;
            uint validId = CanFrameHelper.GetId(canFrame.ID);
            bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);
            bool isRTR = CanFrameHelper.IsRTR(canFrame.ID);

            ChuangXinCanFdLibDLL.ZCAN_TransmitFD_Data sendData = new ChuangXinCanFdLibDLL.ZCAN_TransmitFD_Data();
            sendData.frame = new ChuangXinCanFdLibDLL.canfd_frame
            {
                can_id = ChuangXinCanFdLibDLL.MakeCanId(validId, isExtended, isRTR, false),
                //CANFD 的 len 直接是实际字节数(0-64)
                len = (byte)canFrame.DLC,
                //canfd_frame.flags:bit0=BRS,bit1=ESI
                flags = (byte)(canFrame.BRS ? 0x01 : 0x00),
                data = new byte[DataLenConverter.MaxCanFdDataLength],
            };
            Array.Copy(canFrame.Data, 0, sendData.frame.data, 0, canFrame.DLC);
            sendData.transmit_type = 0;

            Marshal.StructureToPtr(sendData, _txBuffer, true);
            uint result = ChuangXinCanFdLibDLL.ZCAN_TransmitFD(_openedChannelHandle, _txBuffer, 1);
            if (result != 1) return false;

            canFrame.Flags = ChuangXinCanFdFlagParser.Parse(sendData.frame.can_id, sendData.frame.flags, true);
            eventHandler?.Invoke(this, canFrame);
            return true;
        }
    }
}
