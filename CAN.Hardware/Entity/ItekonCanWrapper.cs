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
    /// 爱泰科技(Itekon)经典 CAN 封装(ControlCAN.dll / VCI_ API)。
    /// 适用于 PCI/USB 经典 CAN 系列(USBCAN、CANET、PCI/PCIe CAN 等)。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Itekon, ItekonDeviceType.USBCAN, CommProtocol.CAN)]
    public class ItekonCanWrapper : IHardware
    {
        private readonly ItekonCANConfig _canConfig = new ItekonCANConfig();

        //Open 时的设备参数快照,会话期间固定使用
        private uint _openedDeviceType = 0;
        private uint _openedDeviceIndex = 0;
        private uint _openedChannel = 0;

        //接收缓冲区在 Open 时一次性分配,Close 时释放
        private const int RxBatchSize = 100;    //单次 P/Invoke 最多读取的帧数
        private IntPtr _rxBuffer = IntPtr.Zero;
        private int _rxFrameSize = 0;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _canConfig;

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
                    try { ItekonCanLibDLL.VCI_ResetCAN(_openedDeviceType, _openedDeviceIndex, _openedChannel); } catch { }
                    try { ItekonCanLibDLL.CloseDevice(_openedDeviceType, _openedDeviceIndex); } catch { }
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
                if (_isOpen)
                    ItekonCanLibDLL.VCI_ClearBuffer(_openedDeviceType, _openedDeviceIndex, _openedChannel);
            }
        }

        public void FlushTransmitBuffer()
        {
            lock (_lock)
            {
                if (_isOpen)
                    ItekonCanLibDLL.VCI_ClearBuffer(_openedDeviceType, _openedDeviceIndex, _openedChannel);
            }
        }

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                uint deviceType = (uint)_canConfig.DeviceModel;
                uint deviceIndex = _canConfig.DeviceIndex;
                uint channel = _canConfig.DeviceChannel;
                IntPtr rxBuffer = IntPtr.Zero;
                bool deviceOpened = false;
                bool channelInitialized = false;

                try
                {
                    // 打开设备(同一物理设备的多个通道共享一次 Open)
                    if (!ItekonCanLibDLL.OpenDevice(deviceType, deviceIndex))
                        throw new Exception("Open device failed");
                    deviceOpened = true;

                    // 波特率:优先使用自定义 Timing0/Timing1
                    byte timing0;
                    byte timing1;
                    if (_canConfig.EnableCustomBitrate)
                    {
                        timing0 = _canConfig.Timing0;
                        timing1 = _canConfig.Timing1;
                    }
                    else
                    {
                        ItekonCanLibDLL.BuildTiming(_canConfig.CanBitrate, out timing0, out timing1);
                    }

                    // Filter:0=双滤波,1=单滤波;AccMask 中为 1 的位不参与滤波
                    ItekonCanLibDLL.VCI_INIT_CONFIG initConfig = new ItekonCanLibDLL.VCI_INIT_CONFIG
                    {
                        AccCode = _canConfig.EnableFilter ? _canConfig.AccCode : 0,
                        AccMask = _canConfig.EnableFilter ? _canConfig.AccMask : 0xFFFFFFFF,
                        Reserved = 0,
                        Filter = _canConfig.EnableFilter ? (byte)_canConfig.FilterMode : (byte)ItekonCanFilterMode.Dual,
                        Timing0 = timing0,
                        Timing1 = timing1,
                        Mode = (byte)_canConfig.Mode,
                    };

                    if (ItekonCanLibDLL.VCI_InitCAN(deviceType, deviceIndex, channel, ref initConfig) != 1)
                        throw new Exception("Initialize can channel failed");
                    channelInitialized = true;

                    // 分配持久化接收缓冲区
                    _rxFrameSize = Marshal.SizeOf<ItekonCanLibDLL.VCI_CAN_OBJ>();
                    rxBuffer = Marshal.AllocHGlobal(_rxFrameSize * RxBatchSize);

                    if (ItekonCanLibDLL.VCI_StartCAN(deviceType, deviceIndex, channel) != 1)
                        throw new Exception("Start can channel failed");

                    // 所有操作成功后，才提交状态
                    _openedDeviceType = deviceType;
                    _openedDeviceIndex = deviceIndex;
                    _openedChannel = channel;
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
                    // 未成功初始化的通道不能调用 VCI_ResetCAN(部分驱动会访问空句柄)
                    if (channelInitialized) { try { ItekonCanLibDLL.VCI_ResetCAN(deviceType, deviceIndex, channel); } catch { } }
                    if (deviceOpened) { try { ItekonCanLibDLL.CloseDevice(deviceType, deviceIndex); } catch { } }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _isOpen = false;
                    _rxFrameSize = 0;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
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
                    // 非阻塞批量读取(wait_time=0),单次调用最多取 RxBatchSize 帧
                    uint receiveNum = ItekonCanLibDLL.VCI_Receive(
                        _openedDeviceType, _openedDeviceIndex, _openedChannel,
                        _rxBuffer, RxBatchSize, 0);

                    // 0xFFFFFFFF(-1) 表示读取失败(设备掉线/错误),按无数据处理
                    if (receiveNum == 0 || receiveNum == 0xFFFFFFFF) return false;

                    int count = (int)Math.Min(receiveNum, (uint)RxBatchSize);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr pFrame = IntPtr.Add(_rxBuffer, i * _rxFrameSize);
                        var recData = Marshal.PtrToStructure<ItekonCanLibDLL.VCI_CAN_OBJ>(pFrame);

                        CANFrame frame = new CANFrame();
                        //将 EFF/RTR 标志合成到 ID 高位(SocketCAN 风格)
                        long id = recData.ID & CanFrameDef.CAN_ID_FLAG;
                        if (recData.ExternFlag != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                        if (recData.RemoteFlag != 0) id |= CanFrameDef.CAN_RTR_FLAG;
                        frame.ID = id;

                        //钳制 dlc 到经典 CAN 有效范围，防止驱动返回异常值导致越界
                        frame.DLC = Math.Min((int)recData.DataLen, DataLenConverter.MaxCanDataLength);
                        frame.Flag = (int)id;
                        frame.Flags = ItekonCanFlagParser.Parse(recData.ID, recData.RemoteFlag, recData.ExternFlag);
                        //ControlCAN 时间戳单位为 0.1ms,换算为微秒;TimeFlag=0 表示时间戳无效
                        frame.Timestamp = recData.TimeFlag != 0 ? (ulong)recData.TimeStamp * 100UL : 0UL;

                        frame.Data = new byte[frame.DLC];
                        if (recData.Data != null && frame.DLC > 0)
                            Array.Copy(recData.Data, 0, frame.Data, 0, frame.DLC);
                        frame.Dir = 1;

                        eventHandler?.Invoke(this, frame);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Read)}:{ex.Message}");
                }
            }
        }

        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen) return false;
                if (frame is not CANFrame canFrame) return false;

                //发送前校验数据长度合法性（非法参数直接抛异常，不做静默失败）
                if (canFrame.FDF || canFrame.BRS)
                    throw new ArgumentException("Classic CAN channel does not support CANFD frame(FDF/BRS).");
                if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
                    throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");

                bool isRTR = CanFrameHelper.IsRTR(canFrame.ID);
                if (!isRTR && (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC))
                    throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

                try
                {
                    canFrame.Dir = 0;
                    uint validId = CanFrameHelper.GetId(canFrame.ID);
                    bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);

                    ItekonCanLibDLL.VCI_CAN_OBJ sendObj = new ItekonCanLibDLL.VCI_CAN_OBJ
                    {
                        ID = validId,
                        TimeStamp = 0,
                        TimeFlag = 0,
                        SendType = (byte)_canConfig.SendType,
                        RemoteFlag = (byte)(isRTR ? 1 : 0),
                        ExternFlag = (byte)(isExtended ? 1 : 0),
                        DataLen = (byte)canFrame.DLC,
                        Data = new byte[DataLenConverter.MaxCanDataLength],
                        Reserved = new byte[3],
                    };
                    if (!isRTR && canFrame.DLC > 0)
                        Array.Copy(canFrame.Data, 0, sendObj.Data, 0, canFrame.DLC);

                    //返回实际发送帧数,!=1 视为失败
                    uint result = ItekonCanLibDLL.VCI_Transmit(
                        _openedDeviceType, _openedDeviceIndex, _openedChannel, ref sendObj, 1);
                    if (result != 1) return false;

                    canFrame.Flags = ItekonCanFlagParser.Parse(sendObj.ID, sendObj.RemoteFlag, sendObj.ExternFlag);
                    eventHandler?.Invoke(this, canFrame);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }
    }
}
