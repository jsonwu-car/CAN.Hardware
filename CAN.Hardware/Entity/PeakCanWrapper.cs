using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Peak, PeakDeviceType.Generic, CommProtocol.CAN)]
    public class PeakCanWrapper : IHardware
    {
        //PEAK 通道句柄(TPCANHandle),PCAN_NONEBUS(0) 表示未打开
        private ushort _handle = PeakCanLibDLL.PCAN_NONEBUS;
        private readonly PeakCANConfig _canConfig = new PeakCANConfig();
        private TPCANStatus _peakStatus = TPCANStatus.PCAN_ERROR_OK;

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

                // 先取消Task
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

                // 关闭硬件
                if (_handle != PeakCanLibDLL.PCAN_NONEBUS)
                {
                    try
                    {
                        PeakCanLibDLL.Uninitialize(_handle);
                    }
                    catch { }
                    _handle = PeakCanLibDLL.PCAN_NONEBUS;
                }
                _isOpen = false;
            }
            return true;
        }
        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (_handle != PeakCanLibDLL.PCAN_NONEBUS) PeakCanLibDLL.Reset(_handle);
            }
        }
        public void FlushTransmitBuffer()
        {
            lock (_lock)
            {
                if (_handle != PeakCanLibDLL.PCAN_NONEBUS) PeakCanLibDLL.Reset(_handle);
            }
        }
        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                try
                {
                    // 初始化通道(HwType 仅非即插即用设备使用,默认0)
                    _peakStatus = PeakCanLibDLL.Initialize(_canConfig.CanDeviceChannel, (TPCANBaudrate)(ushort)_canConfig.CanBitrate, (TPCANType)0, _canConfig.IoPort, _canConfig.Interrupt);
                    if (_peakStatus != TPCANStatus.PCAN_ERROR_OK)
                        throw new Exception($"Initialize can failed({_peakStatus.GetEnumDescription()})");

                    // 设置过滤器(PEAK 使用 ID 区间过滤)
                    if (_canConfig.EnableFilter)
                    {
                        TPCANMode mode = _canConfig.Filter_ToId > CanFrameDef.CAN_STD_MAX_ID_FLAG ? TPCANMode.PCAN_MODE_EXTENDED : TPCANMode.PCAN_MODE_STANDARD;
                        _peakStatus = PeakCanLibDLL.FilterMessages(_canConfig.CanDeviceChannel, _canConfig.Filter_FromId, _canConfig.Filter_ToId, mode);
                        if (_peakStatus != TPCANStatus.PCAN_ERROR_OK)
                            throw new Exception($"Set frame filter failed({_peakStatus.GetEnumDescription()})");
                    }

                    // 所有操作成功后，才提交状态
                    _handle = _canConfig.CanDeviceChannel;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    try { PeakCanLibDLL.Uninitialize(_canConfig.CanDeviceChannel); } catch { }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _isOpen = false;
                    _handle = PeakCanLibDLL.PCAN_NONEBUS;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }
        private async Task ReadTaskProc(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    bool res = false;
                    // 连续读取直到无数据
                    do
                    {
                        if (token.IsCancellationRequested) break;
                        CANFrame? frame = null;
                        res = Read(ref frame);
                    }
                    while (res && !token.IsCancellationRequested);

                    // 使用Task.Delay并正确处理取消
                    if (!token.IsCancellationRequested) await Task.Delay(5, token);
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，退出循环
                    break;
                }
                catch (Exception)
                {
                    // 其他异常，稍等后继续
                    try { if (!token.IsCancellationRequested) await Task.Delay(10, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
        private bool Read(ref CANFrame? frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _handle == PeakCanLibDLL.PCAN_NONEBUS) { frame = null; return false; }

                try
                {
                    TPCANMsg canMsg = new TPCANMsg();
                    TPCANTimestamp canTimeStamp = new TPCANTimestamp();
                    _peakStatus = PeakCanLibDLL.Read(_handle, out canMsg, out canTimeStamp);
                    //接收队列为空(PCAN_ERROR_QRCVEMPTY)或其他错误时结束本轮读取
                    if (_peakStatus != TPCANStatus.PCAN_ERROR_OK) return false;

                    uint msgType = (uint)canMsg.MSGTYPE;
                    CANFrame frame1 = new CANFrame();
                    //将 EFF/RTR/ERR 标志合成到 ID 高位(SocketCAN 风格)
                    long id = canMsg.ID;
                    if ((msgType & (uint)TPCANMessageType.PCAN_MESSAGE_EXTENDED) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                    if ((msgType & (uint)TPCANMessageType.PCAN_MESSAGE_RTR) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
                    if ((msgType & (uint)TPCANMessageType.PCAN_MESSAGE_ERRFRAME) != 0) id |= CanFrameDef.CAN_ERR_FLAG;
                    frame1.ID = id;
                    //钳制dlc到经典CAN有效范围，防止驱动返回异常值导致越界
                    frame1.DLC = Math.Min((int)canMsg.LEN, DataLenConverter.MaxCanDataLength);
                    frame1.Flag = (int)msgType;
                    frame1.Flags = PeakCanFlagParser.Parse(msgType);
                    //时间戳合成微秒: micros + 1000*millis + 0x100000000*1000*millis_overflow
                    frame1.Timestamp = (ulong)canTimeStamp.micros + 1000UL * canTimeStamp.millis + 0x100000000UL * 1000UL * canTimeStamp.millis_overflow;
                    frame1.Data = new byte[frame1.DLC];
                    frame1.Dir = 1;
                    Array.Copy(canMsg.DATA, 0, frame1.Data, 0, frame1.DLC);
                    frame = frame1;

                    eventHandler?.Invoke(this, frame);
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
                if (!_isOpen || _handle == PeakCanLibDLL.PCAN_NONEBUS) return false;

                if (frame is not CANFrame canFrame) return false;

                //发送前校验数据长度合法性（非法参数直接抛异常，不做静默失败）
                if (canFrame.FDF || canFrame.BRS)
                    throw new ArgumentException("Classic CAN channel does not support CANFD frame(FDF/BRS).");
                if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
                    throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");
                if (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC)
                    throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

                try
                {
                    canFrame.Dir = 0;
                    TPCANMsg canMsg = new TPCANMsg();
                    canMsg.ID = CanFrameHelper.GetId(canFrame.ID);
                    canMsg.LEN = (byte)canFrame.DLC;
                    //TPCANMsg.DATA 为固定 8 字节数组,需按 SizeConst 分配
                    canMsg.DATA = new byte[DataLenConverter.MaxCanDataLength];
                    Array.Copy(canFrame.Data, 0, canMsg.DATA, 0, canFrame.DLC);
                    canMsg.MSGTYPE = CanFrameHelper.IsEFF(canFrame.ID) ? TPCANMessageType.PCAN_MESSAGE_EXTENDED : TPCANMessageType.PCAN_MESSAGE_STANDARD;
                    if (CanFrameHelper.IsRTR(canFrame.ID)) canMsg.MSGTYPE |= TPCANMessageType.PCAN_MESSAGE_RTR;

                    _peakStatus = PeakCanLibDLL.Write(_handle, ref canMsg);
                    if (_peakStatus != TPCANStatus.PCAN_ERROR_OK) return false;
                    canFrame.Flags = PeakCanFlagParser.Parse((uint)canMsg.MSGTYPE);
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
