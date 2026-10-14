using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Peak, PeakDeviceType.Generic, CommProtocol.CANFD)]
    public class PeakCanFdWrapper : IHardware
    {
        //FD 波特率字符串模板(见 PCAN-Basic InitializeFD 文档)
        private const string BitrateFdFormat = "f_clock={0},nom_brp={1},nom_tseg1={2},nom_tseg2={3},nom_sjw={4},data_brp={5},data_tseg1={6},data_tseg2={7},data_sjw={8}";

        //PEAK 通道句柄(TPCANHandle),PCAN_NONEBUS(0) 表示未打开
        private ushort _handle = PeakCanLibDLL.PCAN_NONEBUS;
        private readonly PeakCanFDConfig _canfdConfig = new PeakCanFDConfig();
        private TPCANStatus _peakStatus = TPCANStatus.PCAN_ERROR_OK;

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
                    // 初始化 FD 通道
                    _peakStatus = PeakCanLibDLL.InitializeFD(_canfdConfig.CanFdDeviceChannel, BuildCanFdBitrate());
                    if (_peakStatus != TPCANStatus.PCAN_ERROR_OK)
                        throw new Exception($"Initialize fd failed({_peakStatus.GetEnumDescription()})");

                    // 设置过滤器(PEAK 使用 ID 区间过滤)
                    if (_canfdConfig.EnableFilter)
                    {
                        TPCANMode mode = _canfdConfig.Filter_ToId > CanFrameDef.CAN_STD_MAX_ID_FLAG ? TPCANMode.PCAN_MODE_EXTENDED : TPCANMode.PCAN_MODE_STANDARD;
                        _peakStatus = PeakCanLibDLL.FilterMessages(_canfdConfig.CanFdDeviceChannel, _canfdConfig.Filter_FromId, _canfdConfig.Filter_ToId, mode);
                        if (_peakStatus != TPCANStatus.PCAN_ERROR_OK)
                            throw new Exception($"Set frame filter failed({_peakStatus.GetEnumDescription()})");
                    }

                    // 所有操作成功后，才提交状态
                    _handle = _canfdConfig.CanFdDeviceChannel;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    try { PeakCanLibDLL.Uninitialize(_canfdConfig.CanFdDeviceChannel); } catch { }
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
                    TPCANMsgFD canfdMsg = new TPCANMsgFD();
                    //TPCANTimestampFD 单位为微秒
                    _peakStatus = PeakCanLibDLL.ReadFD(_handle, out canfdMsg, out ulong timestampFd);
                    //接收队列为空(PCAN_ERROR_QRCVEMPTY)或其他错误时结束本轮读取
                    if (_peakStatus != TPCANStatus.PCAN_ERROR_OK) return false;

                    uint msgType = (uint)canfdMsg.MSGTYPE;
                    CANFrame frame1 = new CANFrame();
                    //将 EFF/RTR/ERR 标志合成到 ID 高位(SocketCAN 风格)
                    long id = canfdMsg.ID;
                    if ((msgType & (uint)TPCANMessageType.PCAN_MESSAGE_EXTENDED) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                    if ((msgType & (uint)TPCANMessageType.PCAN_MESSAGE_RTR) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
                    if ((msgType & (uint)TPCANMessageType.PCAN_MESSAGE_ERRFRAME) != 0) id |= CanFrameDef.CAN_ERR_FLAG;
                    frame1.ID = id;
                    //DLC 编码换算为实际数据长度
                    frame1.DLC = DataLenConverter.DlcToLength(canfdMsg.DLC);
                    frame1.Flag = (int)msgType;
                    frame1.Flags = PeakCanFlagParser.Parse(msgType);
                    frame1.Timestamp = timestampFd;
                    frame1.FDF = (msgType & (uint)TPCANMessageType.PCAN_MESSAGE_FD) != 0;
                    frame1.BRS = (msgType & (uint)TPCANMessageType.PCAN_MESSAGE_BRS) != 0;
                    frame1.Data = new byte[frame1.DLC];
                    frame1.Dir = 1;
                    Array.Copy(canfdMsg.DATA, 0, frame1.Data, 0, frame1.DLC);
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
                if (canFrame.BRS && !canFrame.FDF)
                    throw new ArgumentException("BRS is only valid for CANFD frame(FDF must be set).");
                bool validLength = canFrame.FDF
                    ? DataLenConverter.IsValidCanFdLength(canFrame.DLC)
                    : DataLenConverter.IsValidCanLength(canFrame.DLC);
                if (!validLength)
                    throw new ArgumentException(canFrame.FDF
                        ? $"Invalid CANFD DLC({canFrame.DLC}), valid lengths: 0-8,12,16,20,24,32,48,64."
                        : $"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");
                if (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC)
                    throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

                try
                {
                    canFrame.Dir = 0;
                    TPCANMsgFD canfdMsg = new TPCANMsgFD();
                    canfdMsg.ID = CanFrameHelper.GetId(canFrame.ID);
                    //实际数据长度换算为 DLC 编码(0-15)
                    canfdMsg.DLC = (byte)DataLenConverter.LengthToDlc(canFrame.DLC);
                    //TPCANMsgFD.DATA 为固定 64 字节数组,需按 SizeConst 分配
                    canfdMsg.DATA = new byte[DataLenConverter.MaxCanFdDataLength];
                    Array.Copy(canFrame.Data, 0, canfdMsg.DATA, 0, canFrame.DLC);
                    canfdMsg.MSGTYPE = CanFrameHelper.IsEFF(canFrame.ID) ? TPCANMessageType.PCAN_MESSAGE_EXTENDED : TPCANMessageType.PCAN_MESSAGE_STANDARD;
                    if (CanFrameHelper.IsRTR(canFrame.ID)) canfdMsg.MSGTYPE |= TPCANMessageType.PCAN_MESSAGE_RTR;
                    if (canFrame.FDF) canfdMsg.MSGTYPE |= TPCANMessageType.PCAN_MESSAGE_FD;
                    if (canFrame.BRS) canfdMsg.MSGTYPE |= TPCANMessageType.PCAN_MESSAGE_BRS;

                    _peakStatus = PeakCanLibDLL.WriteFD(_handle, ref canfdMsg);
                    if (_peakStatus != TPCANStatus.PCAN_ERROR_OK) return false;
                    canFrame.Flags = PeakCanFlagParser.Parse((uint)canfdMsg.MSGTYPE);
                    eventHandler?.Invoke(this, canFrame);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }
        private string BuildCanFdBitrate()
        {
            return string.Format(BitrateFdFormat,
                _canfdConfig.Clock,
                _canfdConfig.Arbitration_Brp, _canfdConfig.Arbitration_Tseg1, _canfdConfig.Arbitration_Tseg2, _canfdConfig.Arbitration_Sjw,
                _canfdConfig.Data_Brp, _canfdConfig.Data_Tseg1, _canfdConfig.Data_Tseg2, _canfdConfig.Data_Sjw);
        }
    }
}
