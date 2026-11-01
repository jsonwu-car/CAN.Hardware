using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;
using CAN.Hardware.Parameters;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Esd, EsdDeviceType.Generic, CommProtocol.CANFD)]
    public class EsdCanFdWrapper : IHardware
    {
        //ntcan 通道句柄, Zero 表示未打开
        private IntPtr _handle = IntPtr.Zero;
        private readonly EsdCanFDConfig _esdCanFDConfig = new EsdCanFDConfig();

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _esdCanFDConfig;

        // 使用单一锁保护状态和硬件操作(阻塞读不持锁,见 ReadBlocking)
        private readonly object _lock = new object();

        // Task控制相关
        private bool _isOpen = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        // canReadX 阻塞超时(ms),保证接收线程能周期性检查退出条件
        private const int RX_TIMEOUT_MS = 200;

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                IntPtr tempHandle = IntPtr.Zero;
                try
                {
                    // 定位并加载 ntcan.dll/ntcan64.dll
                    EsdNtcanLibDLL.EnsureLoaded();

                    // 打开 FD 通道; 不传 NO_RTR 以保留经典帧 RTR 接收能力;
                    // txTimeout=0 立即返回, rxTimeout=200ms 支撑接收线程轮询退出
                    var result = EsdNtcanLibDLL.CanOpen(_esdCanFDConfig.Channel, EsdNtcanLibDLL.EsdCanMode.FD,
                        1000, 1000, 0, RX_TIMEOUT_MS, out tempHandle);
                    if (result != EsdNtcanLibDLL.EsdCanResult.OK)
                        throw new Exception($"canOpen failed({result})");
                    // 非连接模式下驱动可能返回 OK 但句柄无效
                    if (tempHandle == IntPtr.Zero || tempHandle.ToInt64() == -1)
                    {
                        tempHandle = IntPtr.Zero;
                        throw new Exception($"canOpen: invalid handle on channel {_esdCanFDConfig.Channel}");
                    }

                    // CAN FD 波特率: canSetBaudrateX 数值模式(NUM),仲裁段/数据段直接填 bps;
                    // 自定义模式下取 CustomArbitrationBitrate/CustomDataBitrate(任意 bps,驱动自动计算位时序)
                    uint arbBitrate = _esdCanFDConfig.EnableCustomBitrate ? _esdCanFDConfig.CustomArbitrationBitrate : (uint)_esdCanFDConfig.CanFdArbitrationBitrate;
                    uint dataBitrate = _esdCanFDConfig.EnableCustomBitrate ? _esdCanFDConfig.CustomDataBitrate : (uint)_esdCanFDConfig.CanFdDataBitrate;
                    if (arbBitrate == 0 || dataBitrate == 0)
                        throw new ArgumentException($"Invalid baudrate(arb={arbBitrate}, data={dataBitrate}), must be greater than 0.");
                    var baudConfig = new EsdNtcanLibDLL.EsdBaudrateXNum
                    {
                        Mode = EsdNtcanLibDLL.BAUDRATE_MODE_NUM,
                        Flags = EsdNtcanLibDLL.BAUDRATE_FLAG_FD,
                        Tdc = new EsdNtcanLibDLL.EsdTdcCfg(),
                        Arb = new EsdNtcanLibDLL.EsdBaudrateCfgNum { Value = arbBitrate },
                        Data = new EsdNtcanLibDLL.EsdBaudrateCfgNum { Value = dataBitrate },
                    };
                    result = EsdNtcanLibDLL.CanSetBaudrateX(tempHandle, baudConfig);
                    if (result != EsdNtcanLibDLL.EsdCanResult.OK)
                        throw new Exception($"Set baudrateX({arbBitrate}/{dataBitrate}) failed({result})");

                    // ntcan 默认关闭所有 ID,需显式使能接收范围: 标准帧 0..0x7FF, 扩展帧 bit29 起全范围
                    EnableAllIdRegions(tempHandle);

                    // 所有操作成功后，才提交状态
                    _handle = tempHandle;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的通道(参考实现此处泄漏句柄)
                    if (tempHandle != IntPtr.Zero)
                    {
                        try { EsdNtcanLibDLL.CanClose(tempHandle); } catch { }
                    }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _handle = IntPtr.Zero;
                    _isOpen = false;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        public bool Close()
        {
            lock (_lock)
            {
                if (!_isOpen) return true;

                // 先取消Task
                if (_cts != null) _cts.Cancel();
            }

            // 在锁外部等待Task完成(canReadX 最坏 200ms 超时后退出),避免死锁
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

                if (_handle != IntPtr.Zero)
                {
                    try { EsdNtcanLibDLL.CanClose(_handle); } catch { }
                    _handle = IntPtr.Zero;
                }
                _isOpen = false;
            }
            return true;
        }

        public void FlushReceiveBuffer()
        {
            // ntcan 子集无队列清空接口,空实现
        }

        public void FlushTransmitBuffer()
        {
            // ntcan 子集无队列清空接口,空实现
        }

        // ntcan 默认关闭所有 ID 的接收,需按 ID 段显式使能(参考实现未检查返回值,此处补齐)
        private static void EnableAllIdRegions(IntPtr handle)
        {
            int cnt = (int)EsdNtcanLibDLL.NTCAN_STD_ID_MASK;   // 0x7FF
            var result = EsdNtcanLibDLL.CanIdRegionAdd(handle, 0, ref cnt);
            if (result != EsdNtcanLibDLL.EsdCanResult.OK)
                throw new Exception($"Enable standard ID region failed({result})");

            cnt = (int)(EsdNtcanLibDLL.NTCAN_EXT_ID_MASK - EsdNtcanLibDLL.NTCAN_STD_ID_MASK);   // 0x1FFFF800
            result = EsdNtcanLibDLL.CanIdRegionAdd(handle, (int)(EsdNtcanLibDLL.NTCAN_EXT_ID_MASK + 1), ref cnt);
            if (result != EsdNtcanLibDLL.EsdCanResult.OK)
                throw new Exception($"Enable extended ID region failed({result})");
        }

        private async Task ReadTaskProc(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // canReadX 阻塞至多有帧或 200ms 超时,无需额外延时
                    ReadBlocking();
                }
                catch (OperationCanceledException)
                {
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

        // 阻塞读不持锁: canReadX 最长阻塞 200ms,持锁会卡住 Send/Close;
        // ntcan 允许同一句柄并发收发, Close 时 canClose 会强制阻塞读返回 HANDLE_FORCED_CLOSE
        private void ReadBlocking()
        {
            if (!_isOpen) return;
            IntPtr handle = _handle;
            if (handle == IntPtr.Zero) return;

            var msg = new EsdNtcanLibDLL.EsdCanFdMsg();
            //Data 为固定 64 字节数组,需按 SizeConst 预分配
            msg.Data = new byte[DataLenConverter.MaxCanFdDataLength];
            int len = 1;
            var result = EsdNtcanLibDLL.CanReadX(handle, ref msg, ref len);
            if (result != EsdNtcanLibDLL.EsdCanResult.OK || len <= 0) return;   // RX_TIMEOUT/强制关闭等直接结束本轮
            if (!_isOpen) return;

            byte rawLen = msg.Base.Len;
            bool isFd = (rawLen & EsdNtcanLibDLL.LEN_FLAG_FD) != 0;
            //DLC 编码换算为实际数据长度,并钳制到CANFD有效范围
            int dlc = Math.Min(DataLenConverter.DlcToLength(rawLen & EsdNtcanLibDLL.LEN_MASK_DLC), DataLenConverter.MaxCanFdDataLength);
            CANFrame frame1 = new CANFrame();
            frame1.ID = EsdCanWrapper.BuildManagedId(msg.Base.Id, rawLen);
            frame1.DLC = dlc;
            frame1.Data = new byte[dlc];
            frame1.Dir = 1;
            frame1.Flag = rawLen;
            frame1.Flags = EsdCanFlagParser.Parse(msg.Base.Id, rawLen, msg.Base.Esi, msg.Base.MsgLost);
            frame1.FDF = isFd;
            frame1.BRS = isFd && (rawLen & EsdNtcanLibDLL.LEN_FLAG_FD_NO_BRS) == 0;
            //ntcan 时间戳单位为微秒,与工程统一单位一致,直接使用
            frame1.Timestamp = msg.Timestamp;
            Array.Copy(msg.Data, 0, frame1.Data, 0, dlc);

            eventHandler?.Invoke(this, frame1);
        }

        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _handle == IntPtr.Zero) return false;

                if (frame is not CANFrame canFrame) return false;

                //发送前校验数据长度合法性（非法参数直接抛异常，不做静默失败）
                if (canFrame.BRS && !canFrame.FDF)
                    throw new ArgumentException("BRS is only valid for CANFD frame(FDF must be set).");
                if (canFrame.FDF && CanFrameHelper.IsRTR(canFrame.ID))
                    throw new ArgumentException("CANFD frame does not support RTR.");
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
                    bool isRtr = CanFrameHelper.IsRTR(canFrame.ID);

                    var msg = new EsdNtcanLibDLL.EsdCanFdMsg();
                    msg.Base.Id = EsdCanWrapper.BuildNtcanId(canFrame.ID);
                    // bit0-3=DLC编码; FD 帧置 bit7,不加速置 bit4(NO_BRS); 经典帧 RTR 置 bit4
                    byte lenByte = (byte)DataLenConverter.LengthToDlc(canFrame.DLC);
                    if (canFrame.FDF)
                    {
                        lenByte |= EsdNtcanLibDLL.LEN_FLAG_FD;
                        if (!canFrame.BRS) lenByte |= EsdNtcanLibDLL.LEN_FLAG_FD_NO_BRS;
                    }
                    else if (isRtr)
                    {
                        lenByte |= EsdNtcanLibDLL.LEN_FLAG_RTR;
                    }
                    msg.Base.Len = lenByte;
                    msg.Data = new byte[DataLenConverter.MaxCanFdDataLength];
                    Array.Copy(canFrame.Data, 0, msg.Data, 0, canFrame.DLC);

                    // FD 通道统一走 canWriteX: 经典帧即不带 FD 位的 X 消息;
                    // txTimeout=0(canOpen 时设定),队列满时 len 回读为 0
                    int len = 1;
                    var result = EsdNtcanLibDLL.CanWriteX(_handle, ref msg, ref len);
                    if (result != EsdNtcanLibDLL.EsdCanResult.OK || len == 0) return false;

                    // 发送成功后回显
                    canFrame.Flag = msg.Base.Len;
                    canFrame.Flags = EsdCanFlagParser.Parse(msg.Base.Id, msg.Base.Len, 0, 0);
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
