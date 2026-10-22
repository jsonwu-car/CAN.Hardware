using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Vector, VectorDeviceType.Generic, CommProtocol.CAN)]
    public class VectorCanWrapper : IHardware
    {
        //Vector 端口句柄,-1 表示未打开
        private int _portHandle = -1;
        private ulong _accessMask = 0;
        private readonly VectorCanConfig _vectorCanConfig = new VectorCanConfig();

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _vectorCanConfig;

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
                    uint hwChannel = _vectorCanConfig.Channel?.HwChannel ?? 0u;
                    if (!VectorCanLibDLL.TryGetChannel(_vectorCanConfig.HardwareType, hwChannel, out XLchannelConfig channelConfig))
                        throw new Exception($"Can not find channel(HwType={_vectorCanConfig.HardwareType}, Channel={hwChannel}). Available: {VectorCanLibDLL.GetCanChannelsText(_vectorCanConfig.HardwareType)}");

                    _accessMask = channelConfig.channelMask;

                    string appName = $"CAN.Hardware_{hwChannel}";
                    status = VectorCanLibDLL.xlSetApplConfig(appName, 0, (ulong)_vectorCanConfig.HardwareType, channelConfig.hwIndex, channelConfig.hwChannel, XLbusType.CAN);
                    if (status != XLstatus.SUCCESS) throw new Exception($"Set appl config failed({status.GetEnumDescription()})");

                    // 打开端口;permissionMask 回传本应用实际获得的通道权限,
                    // 若通道已被其他应用(CANoe等)占用初始化权限,permissionMask 会小于 accessMask
                    ulong permissionMask = _accessMask;
                    status = VectorCanLibDLL.xlOpenPort(ref tempPort, appName, _accessMask, ref permissionMask, 4096, XLinterfaceVersion.V3, XLbusType.CAN);
                    if (status != XLstatus.SUCCESS) throw new Exception($"Open port failed({status.GetEnumDescription()})");

                    // 波特率设置需要初始化权限(通道被其他应用占用时此处拦截)
                    if (_accessMask != permissionMask)
                        throw new Exception($"Init access required to initialize CAN bitrate({XLstatus.ERR_INIT_ACCESS_MISSING.GetEnumDescription()})");

                    // 关闭发送回显事件(TX_REQUEST/TX_COMPLETED),发送帧由Send直接上报
                    status = VectorCanLibDLL.xlCanSetChannelMode(tempPort, _accessMask, 0, 0);
                    if (status != XLstatus.SUCCESS) throw new Exception($"Set channel mode failed({status.GetEnumDescription()})");

                    if (_vectorCanConfig.EnableCustomBitrate)
                    {
                        XLchipParams chipParams = new XLchipParams
                        {
                            bitRate = _vectorCanConfig.CustomBitrate,
                            sjw = (byte)_vectorCanConfig.Sjw,
                            tseg1 = (byte)_vectorCanConfig.Tseg1,
                            tseg2 = (byte)_vectorCanConfig.Tseg2,
                            sam = (byte)_vectorCanConfig.Sam,
                            outputMode = VectorCanLibDLL.XL_OUTPUT_MODE_NORMAL,
                        };
                        status = VectorCanLibDLL.xlCanSetChannelParams(tempPort, _accessMask, ref chipParams);
                        if (status != XLstatus.SUCCESS) throw new Exception($"Set custom bus params failed({status.GetEnumDescription()})");
                    }
                    else
                    {
                        status = VectorCanLibDLL.xlCanSetChannelBitrate(tempPort, _accessMask, (uint)_vectorCanConfig.CanBitrate);
                        if (status != XLstatus.SUCCESS) throw new Exception($"Set {_vectorCanConfig.CanBitrate} failed({status.GetEnumDescription()})");
                    }

                    // 读回校验:确认驱动中实际生效的波特率与请求一致(不一致说明通道/权限/时序配置有误)
                    XLcanBusParamsInfo? busParams = null;
                    if (VectorCanLibDLL.TryReadBusParams(_accessMask, out busParams) && busParams != null && busParams.BitRate != 0)
                    {
                        uint requested = _vectorCanConfig.EnableCustomBitrate ? _vectorCanConfig.CustomBitrate : (uint)_vectorCanConfig.CanBitrate;
                        if (busParams.BitRate != requested)
                            throw new Exception($"Bitrate verify failed on '{busParams.ChannelName}': requested={requested}, effective={busParams.BitRate}(tseg1={busParams.Tseg1},tseg2={busParams.Tseg2},sjw={busParams.Sjw})");
                    }

                    // 时序合理性校验:xlCanSetChannelBitrate 只改波特率,不修正 tseg/sjw;
                    // 若驱动中该应用的时序参数无效(新应用首次注册可能出现),发出的帧无人ACK,必须用完整时序重配
                    if (busParams != null && !IsPlausibleTiming(busParams))
                    {
                        XLchipParams fixParams = new XLchipParams
                        {
                            bitRate = _vectorCanConfig.EnableCustomBitrate ? _vectorCanConfig.CustomBitrate : (uint)_vectorCanConfig.CanBitrate,
                            tseg1 = 12,
                            tseg2 = 3,
                            sjw = 2,
                            sam = 1,
                            outputMode = VectorCanLibDLL.XL_OUTPUT_MODE_NORMAL,
                        };
                        status = VectorCanLibDLL.xlCanSetChannelParams(tempPort, _accessMask, ref fixParams);
                        if (status != XLstatus.SUCCESS)
                            throw new Exception($"Bus timing on '{busParams.ChannelName}' invalid(tseg1={busParams.Tseg1},tseg2={busParams.Tseg2},sjw={busParams.Sjw}) and fix failed({status.GetEnumDescription()})");
                    }

                    // 设置过滤器(Vector 使用 Code/Mask 掩码过滤)
                    if (_vectorCanConfig.EnableFilter)
                    {
                        status = VectorCanLibDLL.xlCanSetAcceptanceFilter(tempPort, _accessMask, _vectorCanConfig.Filter_Code, _vectorCanConfig.Filter_Mask, _vectorCanConfig.Fileter_IsExtended ? 2u : 1u);
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
                    XLevent xlEvent = new XLevent();
                    uint eventCount = 1;
                    XLstatus status = VectorCanLibDLL.xlReceive(_portHandle, ref eventCount, ref xlEvent);
                    //接收队列为空(ERR_QUEUE_IS_EMPTY)或其他错误时结束本轮读取
                    if (status != XLstatus.SUCCESS || eventCount == 0) return false;

                    // 芯片状态事件:上报 busStatus/txErr/rxErr(位于tagData起始3字节,即id低3字节),便于诊断错误帧来源
                    if (xlEvent.tag == (byte)XLeventTag.XL_CHIP_STATE)
                    {
                        CANFrame chipFrame = new CANFrame();
                        chipFrame.ID = CanFrameDef.CAN_ERR_FLAG;
                        chipFrame.DLC = 3;
                        chipFrame.Data = new byte[] { (byte)(xlEvent.id & 0xFF), (byte)((xlEvent.id >> 8) & 0xFF), (byte)((xlEvent.id >> 16) & 0xFF) };
                        chipFrame.Dir = 1;
                        chipFrame.Flag = (int)XLcanMsgFlags.ERROR_FRAME;
                        chipFrame.Flags = VectorCanFlagParser.Parse((uint)XLcanMsgFlags.ERROR_FRAME, 0);
                        //XL时间戳单位为ns，统一转换为us
                        chipFrame.Timestamp = xlEvent.timeStamp / 1000;
                        frame = chipFrame;
                        eventHandler?.Invoke(this, chipFrame);
                        return true;
                    }
                    if (xlEvent.tag != (byte)XLeventTag.XL_RECEIVE_MSG) return false;
                    // 过滤发送回显(TX_COMPLETED/TX_REQUEST),发送帧已在Send中上报
                    if (((XLcanMsgFlags)xlEvent.msgFlags & (XLcanMsgFlags.TX_COMPLETED | XLcanMsgFlags.TX_REQUEST)) != 0) return false;

                    //钳制dlc到经典CAN有效范围，防止驱动返回异常值导致越界
                    int len = Math.Min((int)xlEvent.dlc, DataLenConverter.MaxCanDataLength);
                    CANFrame frame1 = new CANFrame();
                    //将 EFF/RTR/ERR 标志合成到 ID 高位(SocketCAN 风格)
                    long id = xlEvent.id & VectorCanLibDLL.XL_CAN_ID_MASK;
                    if ((xlEvent.id & VectorCanLibDLL.XL_CAN_EXT_MSG_ID) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                    if ((xlEvent.msgFlags & (ushort)XLcanMsgFlags.REMOTE_FRAME) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
                    if ((xlEvent.msgFlags & (ushort)XLcanMsgFlags.ERROR_FRAME) != 0) id |= CanFrameDef.CAN_ERR_FLAG;
                    frame1.ID = id;
                    frame1.DLC = len;
                    frame1.Data = new byte[len];
                    frame1.Dir = 1;
                    frame1.Flag = xlEvent.msgFlags;
                    frame1.Flags = VectorCanFlagParser.Parse(xlEvent.msgFlags, xlEvent.id);
                    //XL时间戳单位为ns，统一转换为us
                    frame1.Timestamp = xlEvent.timeStamp / 1000;
                    //XLevent.data 为8字节小端ulong
                    byte[] raw = BitConverter.GetBytes(xlEvent.data);
                    Array.Copy(raw, 0, frame1.Data, 0, len);
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
                if (canFrame.FDF || canFrame.BRS)
                    throw new ArgumentException("Classic CAN channel does not support CANFD frame(FDF/BRS).");
                if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
                    throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");
                if (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC)
                    throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

                try
                {
                    canFrame.Dir = 0;
                    uint validId = CanFrameHelper.GetId(canFrame.ID);
                    bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);
                    bool isRtr = CanFrameHelper.IsRTR(canFrame.ID);
                    if (isExtended) validId |= VectorCanLibDLL.XL_CAN_EXT_MSG_ID;

                    byte[] bytes = new byte[DataLenConverter.MaxCanDataLength];
                    Array.Copy(canFrame.Data, 0, bytes, 0, canFrame.DLC);

                    XLevent xlEvent = new XLevent();
                    xlEvent.tag = (byte)XLeventTag.XL_TRANSMIT_MSG;
                    // 发送通路由 xlCanTransmit 的 accessMask 决定,事件结构体的 channelIndex 保持 0
                    // (与 Vector 官方 xlCANdemo 及 python-can 一致)
                    xlEvent.id = validId;
                    xlEvent.msgFlags = (ushort)(isRtr ? XLcanMsgFlags.REMOTE_FRAME : XLcanMsgFlags.NONE);
                    xlEvent.dlc = (ushort)canFrame.DLC;
                    xlEvent.data = BitConverter.ToUInt64(bytes, 0);

                    uint msgCnt = 1;
                    XLstatus status = VectorCanLibDLL.xlCanTransmit(_portHandle, _accessMask, ref msgCnt, ref xlEvent);
                    if (status != XLstatus.SUCCESS || msgCnt != 1) return false;
                    // 发送成功后回显,附加 TX_COMPLETED 标志
                    canFrame.Flags = VectorCanFlagParser.Parse((uint)(xlEvent.msgFlags | (ushort)XLcanMsgFlags.TX_COMPLETED), validId);
                    eventHandler?.Invoke(this, canFrame);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }

        // 时序参数是否合理:采样点 50%~95%,tseg1>=tseg2,sjw 1~4 且不超过 tseg2+1
        private static bool IsPlausibleTiming(XLcanBusParamsInfo p)
        {
            if (p.Tseg1 < 2 || p.Tseg2 < 1) return false;
            if (p.Sjw < 1 || p.Sjw > 4 || p.Sjw > p.Tseg2 + 1) return false;
            if (p.Sam != 1 && p.Sam != 3) return false;
            if (p.Tseg1 < p.Tseg2) return false;
            double samplePoint = (double)(1 + p.Tseg1) / (1 + p.Tseg1 + p.Tseg2);
            return samplePoint >= 0.5 && samplePoint <= 0.95;
        }
    }
}
