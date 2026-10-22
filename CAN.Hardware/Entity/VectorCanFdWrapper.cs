using System.Runtime.InteropServices;
using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Vector, VectorDeviceType.Generic, CommProtocol.CANFD)]
    public class VectorCanFdWrapper : IHardware
    {
        //Vector 端口句柄,-1 表示未打开
        private int _portHandle = -1;
        private ulong _accessMask = 0;
        private readonly VectorCanFDConfig _vectorCanFDConfig = new VectorCanFDConfig();

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _vectorCanFDConfig;

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
                if (_portHandle >= 0)
                {
                    try
                    {
                        VectorCanLibDLL.xlDeactivateChannel(_portHandle, _accessMask);
                        VectorCanLibDLL.xlClosePort(_portHandle);
                    }
                    catch { }
                    _portHandle = -1;
                }
                VectorCanLibDLL.CloseDriver();
                _isOpen = false;
            }
            return true;
        }
        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (_portHandle >= 0) VectorCanLibDLL.xlFlushReceiveQueue(_portHandle);
            }
        }
        public void FlushTransmitBuffer()
        {
            lock (_lock)
            {
                if (_portHandle >= 0) VectorCanLibDLL.xlCanFlushTransmitQueue(_portHandle, _accessMask);
            }
        }
        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                int tempPort = -1;
                bool driverOpened = false;
                try
                {
                    // 打开驱动(引用计数,多实例共享)
                    XLstatus status = VectorCanLibDLL.OpenDriver();
                    if (status != XLstatus.SUCCESS) throw new Exception($"Open driver failed({status.GetEnumDescription()})");
                    driverOpened = true;

                    // 按硬件型号+硬件通道号在驱动配置中定位通道,得到 accessMask
                    uint hwChannel = _vectorCanFDConfig.Channel?.HwChannel ?? 0u;
                    if (!VectorCanLibDLL.TryGetChannel(_vectorCanFDConfig.HardwareType, hwChannel, out XLchannelConfig channelConfig))
                        throw new Exception($"Can not find channel(HwType={_vectorCanFDConfig.HardwareType}, Channel={hwChannel}). Available: {VectorCanLibDLL.GetCanChannelsText(_vectorCanFDConfig.HardwareType)}");

                    _accessMask = channelConfig.channelMask;

                    string appName = $"CAN.Hardware_{hwChannel}";
                    status = VectorCanLibDLL.xlSetApplConfig(appName, 0, (ulong)_vectorCanFDConfig.HardwareType, channelConfig.hwIndex, channelConfig.hwChannel, XLbusType.CAN);
                    if (status != XLstatus.SUCCESS) throw new Exception($"Set appl config failed({status.GetEnumDescription()})");

                    // CAN FD 使用 V4 接口,队列大小参考 Vector 示例取 16k;
                    // permissionMask 回传本应用实际获得的通道权限,
                    // 若通道已被其他应用(CANoe等)占用初始化权限,permissionMask 会小于 accessMask
                    ulong permissionMask = _accessMask;
                    status = VectorCanLibDLL.xlOpenPort(ref tempPort, appName, _accessMask, ref permissionMask, 16384, XLinterfaceVersion.V4, XLbusType.CAN);
                    if (status != XLstatus.SUCCESS) throw new Exception($"Open port failed({status.GetEnumDescription()})");

                    // 关闭发送回显事件(TX_REQUEST/TX_OK),发送帧由Send直接上报
                    status = VectorCanLibDLL.xlCanSetChannelMode(tempPort, _accessMask, 0, 0);
                    if (status != XLstatus.SUCCESS) throw new Exception($"Set channel mode failed({status.GetEnumDescription()})");

                    // CAN FD 波特率设置需要初始化权限(通道被其他应用占用时此处拦截)
                    if (_accessMask != permissionMask)
                        throw new Exception($"Init access required to initialize CANFD bitrate({XLstatus.ERR_INIT_ACCESS_MISSING.GetEnumDescription()})");

                    // 标准波特率走内置时序表(BuildFdConf),自定义波特率校验时序合法性后下发
                    if (_vectorCanFDConfig.EnableCustomBitrate)
                    {
                        ValidateFdTiming("arbitration", _vectorCanFDConfig.CustomArbitrationBitrate,
                            _vectorCanFDConfig.Arbitration_tseg1, _vectorCanFDConfig.Arbitration_tseg2, _vectorCanFDConfig.Arbitration_sjw);
                        ValidateFdTiming("data", _vectorCanFDConfig.CustomDataBitrate,
                            _vectorCanFDConfig.Data_tseg1, _vectorCanFDConfig.Data_tseg2, _vectorCanFDConfig.Data_sjw);
                    }
                    XLcanFdConf fdConf = _vectorCanFDConfig.EnableCustomBitrate
                        ? VectorCanLibDLL.BuildFdConfCustom(_vectorCanFDConfig.CustomArbitrationBitrate, _vectorCanFDConfig.Arbitration_tseg1, _vectorCanFDConfig.Arbitration_tseg2, _vectorCanFDConfig.Arbitration_sjw,
                            _vectorCanFDConfig.CustomDataBitrate, _vectorCanFDConfig.Data_tseg1, _vectorCanFDConfig.Data_tseg2, _vectorCanFDConfig.Data_sjw, _vectorCanFDConfig.EnableNonIso)
                        : VectorCanLibDLL.BuildFdConf((uint)_vectorCanFDConfig.CanFdArbitrationBitrate, (uint)_vectorCanFDConfig.CanFdDataBitrate, _vectorCanFDConfig.EnableNonIso);

                    status = VectorCanLibDLL.xlCanFdSetConfiguration(tempPort, _accessMask, ref fdConf);
                    if (status != XLstatus.SUCCESS) throw new Exception($"Set canfd configuration failed({status.GetEnumDescription()})");

                    // 设置过滤器(Vector 使用 Code/Mask 掩码过滤)
                    if (_vectorCanFDConfig.EnableFilter)
                    {
                        status = VectorCanLibDLL.xlCanSetAcceptanceFilter(tempPort, _accessMask, _vectorCanFDConfig.Filter_Code, _vectorCanFDConfig.Filter_Mask, _vectorCanFDConfig.Fileter_IsExtended ? 2u : 1u);
                        if (status != XLstatus.SUCCESS) throw new Exception($"Set frame filter failed({status.GetEnumDescription()})");
                    }

                    // 激活通道(RESET_CLOCK 复位时间基准)
                    status = VectorCanLibDLL.xlActivateChannel(tempPort, _accessMask, XLbusType.CAN, XLactivateFlags.RESET_CLOCK);
                    if (status != XLstatus.SUCCESS) throw new Exception($"Activate channel failed({status.GetEnumDescription()})");

                    // 请求一次芯片状态(经接收事件上报 busStatus/txErr/rxErr),便于诊断总线状态
                    VectorCanLibDLL.xlCanRequestChipState(tempPort, _accessMask);

                    // 所有操作成功后，才提交状态
                    _portHandle = tempPort;
                    tempPort = -1;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源: deactivate -> closePort -> CloseDriver
                    if (tempPort >= 0)
                    {
                        try { VectorCanLibDLL.xlDeactivateChannel(tempPort, _accessMask); VectorCanLibDLL.xlClosePort(tempPort); } catch { }
                    }
                    if (driverOpened) VectorCanLibDLL.CloseDriver();
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _portHandle = -1;
                    _isOpen = false;
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
                if (!_isOpen || _portHandle < 0) { frame = null; return false; }

                try
                {
                    XLcanRxEvent rxEvent = new XLcanRxEvent();
                    rxEvent.size = (uint)Marshal.SizeOf(typeof(XLcanRxEvent));
                    //XLcanRxEvent.data 为固定 64 字节数组,需按 SizeConst 分配
                    rxEvent.data = new byte[DataLenConverter.MaxCanFdDataLength];
                    XLstatus status = VectorCanLibDLL.xlCanReceive(_portHandle, ref rxEvent);
                    //接收队列为空(ERR_QUEUE_IS_EMPTY)或其他错误时结束本轮读取
                    if (status != XLstatus.SUCCESS) return false;

                    // 芯片状态事件:上报 busStatus/txErr/rxErr(位于tagData起始3字节,即canId低3字节),便于诊断错误帧来源
                    if (rxEvent.tag == (ushort)XLcanFdEventTag.CHIP_STATE)
                    {
                        CANFrame chipFrame = new CANFrame();
                        chipFrame.ID = CanFrameDef.CAN_ERR_FLAG;
                        chipFrame.DLC = 3;
                        chipFrame.Data = new byte[] { (byte)(rxEvent.canId & 0xFF), (byte)((rxEvent.canId >> 8) & 0xFF), (byte)((rxEvent.canId >> 16) & 0xFF) };
                        chipFrame.Dir = 1;
                        chipFrame.Flag = (int)XLcanFdRxFlags.EF;
                        chipFrame.Flags = VectorCanFlagParser.ParseFd((uint)XLcanFdRxFlags.EF, 0);
                        //XL时间戳单位为ns，统一转换为us
                        chipFrame.Timestamp = rxEvent.timeStamp / 1000;
                        frame = chipFrame;
                        eventHandler?.Invoke(this, chipFrame);
                        return true;
                    }
                    // 仅处理接收事件,TX_OK/TX_REQUEST 等事件忽略(发送帧已在Send中上报)
                    if (rxEvent.tag != (ushort)XLcanFdEventTag.RX_OK) return false;

                    //DLC 编码换算为实际数据长度,并钳制到CANFD有效范围
                    int len = Math.Min(DataLenConverter.DlcToLength(rxEvent.dlc), DataLenConverter.MaxCanFdDataLength);
                    CANFrame frame1 = new CANFrame();
                    //将 EFF/RTR/ERR 标志合成到 ID 高位(SocketCAN 风格)
                    long id = rxEvent.canId & VectorCanLibDLL.XL_CAN_ID_MASK;
                    if ((rxEvent.canId & VectorCanLibDLL.XL_CAN_EXT_MSG_ID) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                    if ((rxEvent.msgFlags & (uint)XLcanFdRxFlags.RTR) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
                    if ((rxEvent.msgFlags & (uint)XLcanFdRxFlags.EF) != 0) id |= CanFrameDef.CAN_ERR_FLAG;
                    frame1.ID = id;
                    frame1.DLC = len;
                    frame1.Data = new byte[len];
                    frame1.Dir = 1;
                    frame1.Flag = (int)rxEvent.msgFlags;
                    frame1.Flags = VectorCanFlagParser.ParseFd(rxEvent.msgFlags, rxEvent.canId);
                    frame1.FDF = (rxEvent.msgFlags & (uint)XLcanFdRxFlags.EDL) != 0;
                    frame1.BRS = (rxEvent.msgFlags & (uint)XLcanFdRxFlags.BRS) != 0;
                    //XL时间戳单位为ns，统一转换为us
                    frame1.Timestamp = rxEvent.timeStamp / 1000;
                    Array.Copy(rxEvent.data, 0, frame1.Data, 0, len);
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
                if (!_isOpen || _portHandle < 0) return false;

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
                    uint validId = CanFrameHelper.GetId(canFrame.ID);
                    bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);
                    bool isRtr = CanFrameHelper.IsRTR(canFrame.ID);
                    if (isExtended) validId |= VectorCanLibDLL.XL_CAN_EXT_MSG_ID;

                    uint txFlags = (uint)XLcanTxMsgFlags.NONE;
                    if (canFrame.FDF) txFlags |= (uint)XLcanTxMsgFlags.EDL;
                    if (canFrame.BRS) txFlags |= (uint)XLcanTxMsgFlags.BRS;
                    if (isRtr) txFlags |= (uint)XLcanTxMsgFlags.RTR;

                    XLcanTxEvent txEvent = new XLcanTxEvent();
                    txEvent.tag = (ushort)XLcanFdEventTag.TX_MSG;
                    txEvent.transId = 0xFFFF;
                    // 发送通路由 xlCanTransmitEx 的 accessMask 决定,channelIndex 保持 0
                    txEvent.id = validId;
                    txEvent.flags = txFlags;
                    //实际数据长度换算为 DLC 编码(0-15)
                    txEvent.dlc = (byte)DataLenConverter.LengthToDlc(canFrame.DLC);
                    //XLcanTxEvent.reserved3/data 为固定 7/64 字节数组,需按 SizeConst 分配
                    txEvent.reserved3 = new byte[7];
                    txEvent.data = new byte[DataLenConverter.MaxCanFdDataLength];
                    Array.Copy(canFrame.Data, 0, txEvent.data, 0, canFrame.DLC);

                    uint msgCntSent = 0;
                    XLcanTxEvent[] txEvents = new XLcanTxEvent[] { txEvent };
                    XLstatus status = VectorCanLibDLL.xlCanTransmitEx(_portHandle, _accessMask, 1, ref msgCntSent, txEvents);
                    if (status != XLstatus.SUCCESS || msgCntSent != 1) return false;
                    // 发送成功后回显,标记发送确认
                    canFrame.Flags = VectorCanFlagParser.ParseFd(txFlags, validId, true);
                    eventHandler?.Invoke(this, canFrame);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }

        // 自定义FD时序参数合法性校验:采样点 50%~95%,tseg1>=tseg2,sjw>=1 且不超过 tseg2
        private static void ValidateFdTiming(string segment, uint bitrate, int tseg1, int tseg2, int sjw)
        {
            if (bitrate == 0) throw new ArgumentException($"Invalid CANFD {segment} bitrate(0).");
            if (tseg1 < 1 || tseg2 < 1 || tseg1 < tseg2)
                throw new ArgumentException($"Invalid CANFD {segment} timing: tseg1({tseg1})/tseg2({tseg2}) out of range(tseg1>=tseg2>=1).");
            if (sjw < 1 || sjw > tseg2)
                throw new ArgumentException($"Invalid CANFD {segment} timing: sjw({sjw}) out of range(1~tseg2={tseg2}).");
            double samplePoint = (double)(1 + tseg1) / (1 + tseg1 + tseg2);
            if (samplePoint < 0.5 || samplePoint > 0.95)
                throw new ArgumentException($"Invalid CANFD {segment} timing: sample point({samplePoint:P1}) out of range(50%~95%).");
        }
    }
}
