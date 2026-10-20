using System.Runtime.InteropServices;
using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    //USBCANFD-200U 同时支持 CAN 与 CANFD,由 ZlgCanFDConfig.UseCanFD 决定工作模式
    [HardwareDefAttributes(HardwareManufacturer.Zlg, ZlgDeviceType.USBCANFD200U, CommProtocol.CANFD)]
    [HardwareDefAttributes(HardwareManufacturer.Zlg, ZlgDeviceType.USBCANFD200U, CommProtocol.CAN)]
    public class ZlgCanFdWrapper : IHardware
    {
        //ZLG 句柄分两层: 设备句柄(共享,引用计数) + 通道句柄,IntPtr.Zero 表示未打开
        private IntPtr _devHandle = IntPtr.Zero;
        private IntPtr _channelHandle = IntPtr.Zero;
        private readonly ZlgCanFDConfig _canfdConfig = new ZlgCanFDConfig();

        //Open 时的模式快照,会话期间收发 API 与缓冲区布局必须与其保持一致
        private bool _openedUseCanFD = true;

        //接收/发送缓冲区在 Open 时一次性分配,Close 时释放,避免每帧 AllocHGlobal/FreeHGlobal
        private const int RxBatchSize = 100;    //单次 P/Invoke 最多读取的帧数
        private IntPtr _rxBuffer = IntPtr.Zero;
        private int _rxFrameSize = 0;
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
                if (_channelHandle != IntPtr.Zero)
                {
                    try
                    {
                        //清除设备上可能残留的定时发送配置(best-effort,不阻塞关闭流程)
                        if (_devHandle != IntPtr.Zero)
                            ZLGCanLibDLL.ZCAN_SetValue(_devHandle, $"{_canfdConfig.DeviceChannel}/clear_auto_send", "0");
                        ZLGCanLibDLL.ZCAN_ResetCAN(_channelHandle);
                    }
                    catch { }
                    _channelHandle = IntPtr.Zero;
                }
                if (_devHandle != IntPtr.Zero)
                {
                    try { ZLGCanLibDLL.CloseDevice(); } catch { }
                    _devHandle = IntPtr.Zero;
                }
                FreeBuffers();
                _isOpen = false;
            }
            return true;
        }
        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (_channelHandle != IntPtr.Zero) ZLGCanLibDLL.ZCAN_ClearBuffer(_channelHandle);
            }
        }
        public void FlushTransmitBuffer()
        {
            lock (_lock)
            {
                if (_channelHandle != IntPtr.Zero) ZLGCanLibDLL.ZCAN_ClearBuffer(_channelHandle);
            }
        }
        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                IntPtr tempDevHandle = IntPtr.Zero;
                IntPtr tempChannelHandle = IntPtr.Zero;
                IntPtr tempRxBuffer = IntPtr.Zero;
                IntPtr tempTxBuffer = IntPtr.Zero;
                try
                {
                    bool useCanFD = _canfdConfig.UseCanFD;

                    // 打开设备(同一物理设备共享句柄,多通道复用)
                    // 注意:ZLG 返回值约定 1=成功(STATUS_OK),0=失败;OpenDevice/InitCAN 返回句柄,Zero=失败
                    tempDevHandle = ZLGCanLibDLL.OpenDevice((uint)_canfdConfig.DeviceModel, _canfdConfig.DeviceIndex, 0);
                    if (tempDevHandle == IntPtr.Zero) throw new Exception("Open device failed");

                    // CAN FD 标准(ISO/BOSCH),仅 CANFD 模式需要
                    uint status = 1;
                    if (useCanFD)
                    {
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/canfd_standard", ((int)_canfdConfig.CANFDStandard).ToString());
                        if (status != 1) throw new Exception($"Set canfd standard failed({status})");

                        // 关闭合并接收,保证每帧独立上报、时间戳准确(best-effort,部分固件不支持该路径)
                        ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/set_device_recv_merge", "0");
                    }

                    // 设置波特率(InitCAN 之前)
                    if (_canfdConfig.EnableCustomBitrate)
                    {
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/baud_rate_custom", _canfdConfig.CustomBitrateContent);
                        if (status != 1) throw new Exception($"Set custom baud rate failed({status})");
                    }
                    else if (useCanFD)
                    {
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/canfd_abit_baud_rate", ZLGCanLibDLL.BuildCanBitrate(_canfdConfig.ArbitrationBitrate).ToString());
                        if (status != 1) throw new Exception($"Set {_canfdConfig.ArbitrationBitrate} failed({status})");
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/canfd_dbit_baud_rate", ZLGCanLibDLL.BuildCanFdBitrate(_canfdConfig.DataBitrate).ToString());
                        if (status != 1) throw new Exception($"Set {_canfdConfig.DataBitrate} failed({status})");
                    }
                    else
                    {
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/baud_rate", ZLGCanLibDLL.BuildCanBitrate(_canfdConfig.ArbitrationBitrate).ToString());
                        if (status != 1) throw new Exception($"Set {_canfdConfig.ArbitrationBitrate} failed({status})");
                    }

                    // 初始化通道(按模式选择 can_type 与对应的联合体配置)
                    ZLGCanLibDLL.ZCAN_CHANNEL_INIT_CONFIG initConfig = new ZLGCanLibDLL.ZCAN_CHANNEL_INIT_CONFIG();
                    initConfig.can_type = useCanFD ? 1u : 0u;
                    initConfig.config.can.acc_code = 0;
                    initConfig.config.can.acc_mask = 0xFFFFFFFF;
                    initConfig.config.can.mode = 0;
                    initConfig.config.canfd.acc_code = 0;
                    initConfig.config.canfd.acc_mask = 0xFFFFFFFF;
                    initConfig.config.canfd.mode = 0;
                    IntPtr pInitConfig = Marshal.AllocHGlobal(Marshal.SizeOf(initConfig));
                    try
                    {
                        Marshal.StructureToPtr(initConfig, pInitConfig, true);
                        tempChannelHandle = ZLGCanLibDLL.ZCAN_InitCAN(tempDevHandle, _canfdConfig.DeviceChannel, pInitConfig);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pInitConfig);
                    }
                    if (tempChannelHandle == IntPtr.Zero) throw new Exception("Initialize channel failed");

                    // 终端电阻(InitCAN 之后设置)
                    status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/initenal_resistance", _canfdConfig.EnableA120Ohm ? "1" : "0");
                    if (status != 1) throw new Exception($"Set internal resistance failed({status})");

                    // 设置过滤器(ZLG 使用 ID 区间过滤)
                    if (_canfdConfig.EnableFilter)
                    {
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/filter_mode", _canfdConfig.Filter_ToId > CanFrameDef.CAN_STD_MAX_ID_FLAG ? "1" : "0");
                        if (status != 1) throw new Exception($"Set filter mode failed({status})");
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/filter_start", $"0x{_canfdConfig.Filter_FromId:X2}");
                        if (status != 1) throw new Exception($"Set filter start failed({status})");
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/filter_end", $"0x{_canfdConfig.Filter_ToId:X2}");
                        if (status != 1) throw new Exception($"Set filter end failed({status})");
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/filter_ack", "0");
                        if (status != 1) throw new Exception($"Set filter ack failed({status})");
                    }
                    else
                    {
                        status = ZLGCanLibDLL.ZCAN_SetValue(tempDevHandle, $"{_canfdConfig.DeviceChannel}/filter_clear", "0");
                        if (status != 1) throw new Exception($"Clear filter failed({status})");
                    }

                    // 分配持久化收发缓冲区(收发结构体均按当前模式布局,FD 结构体更大)
                    int rxFrameSize = useCanFD
                        ? Marshal.SizeOf(typeof(ZLGCanLibDLL.ZCAN_ReceiveFD_Data))
                        : Marshal.SizeOf(typeof(ZLGCanLibDLL.ZCAN_Receive_Data));
                    tempRxBuffer = Marshal.AllocHGlobal(rxFrameSize * RxBatchSize);
                    tempTxBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ZLGCanLibDLL.ZCAN_TransmitFD_Data)));

                    // 启动通道
                    status = ZLGCanLibDLL.ZCAN_StartCAN(tempChannelHandle);
                    if (status != 1) throw new Exception($"Start channel failed({status})");

                    // 所有操作成功后，才提交状态
                    _devHandle = tempDevHandle;
                    _channelHandle = tempChannelHandle;
                    _openedUseCanFD = useCanFD;
                    _rxBuffer = tempRxBuffer;
                    _rxFrameSize = rxFrameSize;
                    _txBuffer = tempTxBuffer;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (tempRxBuffer != IntPtr.Zero) Marshal.FreeHGlobal(tempRxBuffer);
                    if (tempTxBuffer != IntPtr.Zero) Marshal.FreeHGlobal(tempTxBuffer);
                    if (tempChannelHandle != IntPtr.Zero) { try { ZLGCanLibDLL.ZCAN_ResetCAN(tempChannelHandle); } catch { } }
                    if (tempDevHandle != IntPtr.Zero) { try { ZLGCanLibDLL.CloseDevice(); } catch { } }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _isOpen = false;
                    _channelHandle = IntPtr.Zero;
                    _devHandle = IntPtr.Zero;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }
        private void FreeBuffers()
        {
            if (_rxBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(_rxBuffer); _rxBuffer = IntPtr.Zero; }
            if (_txBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(_txBuffer); _txBuffer = IntPtr.Zero; }
            _rxFrameSize = 0;
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
                        res = Read();
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
        private bool Read()
        {
            lock (_lock)
            {
                if (!_isOpen || _channelHandle == IntPtr.Zero || _rxBuffer == IntPtr.Zero) return false;

                try
                {
                    // 非阻塞批量读取(wait_time=0),单次调用最多取 RxBatchSize 帧
                    uint receiveNum = _openedUseCanFD
                        ? ZLGCanLibDLL.ZCAN_ReceiveFD(_channelHandle, _rxBuffer, RxBatchSize, 0)
                        : ZLGCanLibDLL.ZCAN_Receive(_channelHandle, _rxBuffer, RxBatchSize, 0);
                    if (receiveNum == 0) return false;

                    for (int i = 0; i < receiveNum; i++)
                    {
                        IntPtr pFrame = IntPtr.Add(_rxBuffer, i * _rxFrameSize);
                        CANFrame? frame = _openedUseCanFD ? ParseFdFrame(pFrame) : ParseCanFrame(pFrame);
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
        private static CANFrame? ParseFdFrame(IntPtr pFrame)
        {
            var recData = Marshal.PtrToStructure<ZLGCanLibDLL.ZCAN_ReceiveFD_Data>(pFrame);
            if (recData?.frame == null) return null;

            CANFrame frame = new CANFrame();
            //ZLG 的 can_id 高位自带 SocketCAN 风格 EFF/RTR/ERR 标志,直接透传
            frame.ID = recData.frame.can_id;
            //ZLG FD 帧 len 直接是实际字节数(0-64),无需 DLC 编码换算;钳制防止驱动返回异常值
            frame.DLC = Math.Min((int)recData.frame.len, DataLenConverter.MaxCanFdDataLength);
            frame.Flag = (int)recData.frame.can_id;
            frame.Flags = ZlgCanFlagParser.Parse(recData.frame.can_id, recData.frame.flags, true);
            //ZLG 时间戳单位为 0.1ms,换算为微秒
            frame.Timestamp = recData.timestamp * 100;
            //ZLG FD 帧 flags 只有 bit0=BRS/bit1=ESI,无 FDF 位;BRS 置位或长度>8 即判定为 FD 帧
            frame.BRS = (recData.frame.flags & 0x01) != 0;
            frame.FDF = frame.BRS || frame.DLC > DataLenConverter.MaxCanDataLength;
            frame.Data = new byte[frame.DLC];
            frame.Dir = 1;
            Array.Copy(recData.frame.data, 0, frame.Data, 0, frame.DLC);
            return frame;
        }
        private static CANFrame? ParseCanFrame(IntPtr pFrame)
        {
            var recData = Marshal.PtrToStructure<ZLGCanLibDLL.ZCAN_Receive_Data>(pFrame);
            if (recData?.frame == null) return null;

            CANFrame frame = new CANFrame();
            //ZLG 的 can_id 高位自带 SocketCAN 风格 EFF/RTR/ERR 标志,直接透传
            frame.ID = recData.frame.can_id;
            //钳制dlc到经典CAN有效范围，防止驱动返回异常值导致越界
            frame.DLC = Math.Min((int)recData.frame.can_dlc, DataLenConverter.MaxCanDataLength);
            frame.Flag = (int)recData.frame.can_id;
            frame.Flags = ZlgCanFlagParser.Parse(recData.frame.can_id, 0, false);
            //ZLG 时间戳单位为 0.1ms,换算为微秒
            frame.Timestamp = recData.timestamp * 100;
            frame.Data = new byte[frame.DLC];
            frame.Dir = 1;
            Array.Copy(recData.frame.data, 0, frame.Data, 0, frame.DLC);
            return frame;
        }
        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _channelHandle == IntPtr.Zero || _txBuffer == IntPtr.Zero) return false;

                if (frame is not CANFrame canFrame) return false;

                try
                {
                    //FD 模式下 ZCAN_TransmitFD 同时兼容经典 CAN 帧(flags=0 且 len<=8)
                    return _openedUseCanFD ? SendFd(canFrame) : SendCan(canFrame);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }
        private bool SendCan(CANFrame canFrame)
        {
            //发送前校验数据长度合法性（非法参数直接抛异常，不做静默失败）
            if (canFrame.FDF || canFrame.BRS)
                throw new ArgumentException("Classic CAN mode does not support CANFD frame(FDF/BRS).");
            if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
                throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");
            if (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC)
                throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

            canFrame.Dir = 0;
            var validId = CanFrameHelper.GetId(canFrame.ID);
            var isExtended = CanFrameHelper.IsEFF(canFrame.ID);
            var isRTR = CanFrameHelper.IsRTR(canFrame.ID);

            ZLGCanLibDLL.ZCAN_Transmit_Data canData = new ZLGCanLibDLL.ZCAN_Transmit_Data();
            canData.frame = new ZLGCanLibDLL.can_frame();
            canData.frame.can_id = ZLGCanLibDLL.MakeCanId(validId, isExtended ? 1 : 0, isRTR, false);
            canData.frame.can_dlc = (byte)canFrame.DLC;
            //can_frame.data 为固定 8 字节数组,需按 SizeConst 分配
            canData.frame.data = new byte[DataLenConverter.MaxCanDataLength];
            Array.Copy(canFrame.Data, 0, canData.frame.data, 0, canFrame.DLC);
            canData.transmit_type = 0;

            Marshal.StructureToPtr(canData, _txBuffer, true);
            //返回实际发送帧数,!=1 视为失败
            uint result = ZLGCanLibDLL.ZCAN_Transmit(_channelHandle, _txBuffer, 1);
            if (result != 1) return false;
            canFrame.Flags = ZlgCanFlagParser.Parse(canData.frame.can_id, 0, false);
            eventHandler?.Invoke(this, canFrame);
            return true;
        }
        private bool SendFd(CANFrame canFrame)
        {
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

            canFrame.Dir = 0;
            var validId = CanFrameHelper.GetId(canFrame.ID);
            var isExtended = CanFrameHelper.IsEFF(canFrame.ID);
            var isRTR = CanFrameHelper.IsRTR(canFrame.ID);

            ZLGCanLibDLL.ZCAN_TransmitFD_Data canfdData = new ZLGCanLibDLL.ZCAN_TransmitFD_Data();
            canfdData.frame = new ZLGCanLibDLL.canfd_frame();
            canfdData.frame.can_id = ZLGCanLibDLL.MakeCanId(validId, isExtended ? 1 : 0, isRTR, false);
            //ZLG FD 帧 len 直接是实际字节数(0-64),无需 DLC 编码换算
            canfdData.frame.len = (byte)canFrame.DLC;
            //canfd_frame.flags: bit0=BRS,bit1=ESI
            canfdData.frame.flags = (byte)(canFrame.BRS ? 0x01 : 0x00);
            //canfd_frame.data 为固定 64 字节数组,需按 SizeConst 分配
            canfdData.frame.data = new byte[DataLenConverter.MaxCanFdDataLength];
            Array.Copy(canFrame.Data, 0, canfdData.frame.data, 0, canFrame.DLC);
            canfdData.transmit_type = 0;

            Marshal.StructureToPtr(canfdData, _txBuffer, true);
            //返回实际发送帧数,!=1 视为失败
            uint result = ZLGCanLibDLL.ZCAN_TransmitFD(_channelHandle, _txBuffer, 1);
            if (result != 1) return false;
            canFrame.Flags = ZlgCanFlagParser.Parse(canfdData.frame.can_id, canfdData.frame.flags, canFrame.FDF);
            eventHandler?.Invoke(this, canFrame);
            return true;
        }
    }
}
