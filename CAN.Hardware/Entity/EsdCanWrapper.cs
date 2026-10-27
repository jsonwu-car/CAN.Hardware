using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;
using CAN.Hardware.Parameters;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Esd, EsdDeviceType.Generic, CommProtocol.CAN)]
    public class EsdCanWrapper : IHardware
    {
        //ntcan 通道句柄, Zero 表示未打开
        private IntPtr _handle = IntPtr.Zero;
        private readonly EsdCANConfig _esdCanConfig = new EsdCANConfig();

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _esdCanConfig;

        // 使用单一锁保护状态和硬件操作(阻塞读不持锁,见 ReadBlocking)
        private readonly object _lock = new object();

        // Task控制相关
        private bool _isOpen = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        // canReadT 阻塞超时(ms),保证接收线程能周期性检查退出条件
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

                    // 打开通道; 不传 NO_RTR 以保留 RTR 接收能力;
                    // txTimeout=0 立即返回, rxTimeout=200ms 支撑接收线程轮询退出
                    var result = EsdNtcanLibDLL.CanOpen(_esdCanConfig.Channel, EsdNtcanLibDLL.EsdCanMode.NONE,
                        1000, 1000, 0, RX_TIMEOUT_MS, out tempHandle);
                    if (result != EsdNtcanLibDLL.EsdCanResult.OK)
                        throw new Exception($"canOpen failed({result})");
                    // 非连接模式下驱动可能返回 OK 但句柄无效
                    if (tempHandle == IntPtr.Zero || tempHandle.ToInt64() == -1)
                    {
                        tempHandle = IntPtr.Zero;
                        throw new Exception($"canOpen: invalid handle on channel {_esdCanConfig.Channel}");
                    }

                    // 数值波特率: USER_BAUDRATE_NUM | bps; 自定义模式下取 CustomBitrate(任意 bps,驱动自动计算位时序)
                    uint bitrate = _esdCanConfig.EnableCustomBitrate ? _esdCanConfig.CustomBitrate : (uint)_esdCanConfig.CanBitrate;
                    // USER_BAUDRATE_NUM 模式下低 28 位为 bps 数值,0 或超出 28 位直接拦截
                    if (bitrate == 0 || bitrate > 0x0FFFFFFF)
                        throw new ArgumentException($"Invalid bitrate({bitrate}), valid range: 1-268435455 bps.");
                    result = EsdNtcanLibDLL.CanSetBaudrate(tempHandle, (uint)EsdNtcanLibDLL.EsdBaudMode.USER_BAUDRATE_NUM | bitrate);
                    if (result != EsdNtcanLibDLL.EsdCanResult.OK)
                        throw new Exception($"Set baudrate {bitrate} failed({result})");

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

            // 在锁外部等待Task完成(canReadT 最坏 200ms 超时后退出),避免死锁
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
                    // canReadT 阻塞至多有帧或 200ms 超时,无需额外延时
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

        // 阻塞读不持锁: canReadT 最长阻塞 200ms,持锁会卡住 Send/Close;
        // ntcan 允许同一句柄并发收发, Close 时 canClose 会强制阻塞读返回 HANDLE_FORCED_CLOSE
        private void ReadBlocking()
        {
            if (!_isOpen) return;
            IntPtr handle = _handle;
            if (handle == IntPtr.Zero) return;

            var msg = new EsdNtcanLibDLL.EsdCanMsg();
            int len = 1;
            var result = EsdNtcanLibDLL.CanRead(handle, ref msg, ref len);
            if (result != EsdNtcanLibDLL.EsdCanResult.OK || len <= 0) return;   // RX_TIMEOUT/强制关闭等直接结束本轮
            if (!_isOpen) return;

            // 钳制dlc到经典CAN有效范围，防止驱动返回异常值导致越界
            byte rawLen = msg.Base.Len;
            int dlc = Math.Min(rawLen & EsdNtcanLibDLL.LEN_MASK_DLC, DataLenConverter.MaxCanDataLength);
            CANFrame frame1 = new CANFrame();
            frame1.ID = BuildManagedId(msg.Base.Id, rawLen);
            frame1.DLC = dlc;
            frame1.Data = new byte[dlc];
            frame1.Dir = 1;
            frame1.Flag = rawLen;
            frame1.Flags = EsdCanFlagParser.Parse(msg.Base.Id, rawLen, msg.Base.Esi, msg.Base.MsgLost);
            //ntcan 时间戳单位为微秒,与工程统一单位一致,直接使用
            frame1.Timestamp = msg.Timestamp;
            //Data 为 8 字节小端 ulong
            byte[] raw = BitConverter.GetBytes(msg.Data);
            Array.Copy(raw, 0, frame1.Data, 0, dlc);

            eventHandler?.Invoke(this, frame1);
        }

        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _handle == IntPtr.Zero) return false;

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
                    bool isRtr = CanFrameHelper.IsRTR(canFrame.ID);

                    var msg = new EsdNtcanLibDLL.EsdCanMsg();
                    msg.Base.Id = BuildNtcanId(canFrame.ID);
                    // bit0-3=DLC, bit4=RTR
                    msg.Base.Len = (byte)(canFrame.DLC | (isRtr ? EsdNtcanLibDLL.LEN_FLAG_RTR : 0));
                    byte[] bytes = new byte[DataLenConverter.MaxCanDataLength];
                    Array.Copy(canFrame.Data, 0, bytes, 0, canFrame.DLC);
                    msg.Data = BitConverter.ToUInt64(bytes, 0);

                    // txTimeout=0(canOpen 时设定),队列满时 len 回读为 0
                    int len = 1;
                    var result = EsdNtcanLibDLL.CanWrite(_handle, ref msg, ref len);
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

        // 托管层 SocketCAN 高位标志(bit31=EFF, bit30=RTR) -> ntcan 原生 ID(bit29=EFF, RTR 在 Len)
        internal static uint BuildNtcanId(long id)
        {
            uint result = CanFrameHelper.GetId(id);
            if (CanFrameHelper.IsEFF(id)) result |= EsdNtcanLibDLL.NTCAN_EXT_FLAG;
            return result;
        }

        // ntcan 原生 ID + Len -> 合成 SocketCAN 高位标志的托管 ID
        internal static long BuildManagedId(uint ntcanId, byte len)
        {
            long id = ntcanId & EsdNtcanLibDLL.NTCAN_EXT_ID_MASK;
            if ((ntcanId & EsdNtcanLibDLL.NTCAN_EXT_FLAG) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
            bool isFd = (len & EsdNtcanLibDLL.LEN_FLAG_FD) != 0;
            if (!isFd && (len & EsdNtcanLibDLL.LEN_FLAG_RTR) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
            return id;
        }
    }
}
