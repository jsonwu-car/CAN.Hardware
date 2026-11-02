using System.Runtime.InteropServices;
using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.EbEl, EbElDeviceType.Generic, CommProtocol.CAN)]
    public class EbElCanWrapper : IHardware
    {
        //FlexCard 设备句柄(IntPtr.Zero 表示未打开)
        private IntPtr _handle = IntPtr.Zero;
        private readonly EbElCANConfig _canConfig = new EbElCANConfig();

        // 通道号(0基) -> 通信控制器(fcCC1~fcCC8), 连续可直接强转, 这里只做范围校验
        private EbElFcbaseLibDLL.fcCC _cc = EbElFcbaseLibDLL.fcCC.CC1;

        // 接收轮询空闲间隔(ms): fcbReceive 为"尝试取"语义, 无报文时小睡避免空转
        private const int RxPollIdleMs = 5;

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

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                IntPtr tempHandle = IntPtr.Zero;
                try
                {
                    EbElFcbaseLibDLL.EnsureLoaded();

                    if (_canConfig.Channel > (uint)EbElFcbaseLibDLL.fcCC.CC8)
                        throw new ArgumentException($"Invalid channel({_canConfig.Channel}), valid range: 0-{(uint)EbElFcbaseLibDLL.fcCC.CC8}(CC1~CC8).");
                    _cc = (EbElFcbaseLibDLL.fcCC)_canConfig.Channel;

                    // 枚举第一块带 CAN 控制器的 FlexCard(参考实现在构造函数里枚举,移到 Open 按需执行)
                    uint flexCardId = EbElFcbaseLibDLL.FindFirstCanCard();
                    if (flexCardId == 0)
                        throw new Exception("No FlexCard with CAN controller found,请确认已安装 FlexCard 驱动并连接设备");

                    var result = EbElFcbaseLibDLL.Open(flexCardId, out tempHandle);
                    if (result != EbElFcbaseLibDLL.fcResult.OK || tempHandle == IntPtr.Zero)
                    {
                        tempHandle = IntPtr.Zero;
                        throw new Exception($"fcbOpen failed({result})");
                    }

                    // 波特率: fcbase 无数值接口, 按枚举查位时序表组 fcCANCcConfig
                    var ccConfig = BuildCcConfig(_canConfig.CanBitrate);
                    result = EbElFcbaseLibDLL.SetCcConfiguration(tempHandle, _cc, ccConfig);
                    if (result != EbElFcbaseLibDLL.fcResult.OK)
                        throw new Exception($"fcbCANSetCcConfiguration failed({result})");

                    // 启动监控(resetTimestamps=1, Normal 模式); 此后 fcbReceive 才有报文
                    result = EbElFcbaseLibDLL.MonitoringStart(tempHandle, _cc);
                    if (result != EbElFcbaseLibDLL.fcResult.OK)
                        throw new Exception($"fcbCANMonitoringStart failed({result})");

                    // 所有操作成功后，才提交状态
                    _handle = tempHandle;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源(参考实现此处直接泄漏句柄)
                    if (tempHandle != IntPtr.Zero) { try { EbElFcbaseLibDLL.Close(tempHandle); } catch { } }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _isOpen = false;
                    _handle = IntPtr.Zero;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 按波特率枚举组位时序(参考实现同款查表)。
        /// 除 800K 外各档均满足 12MHz / (Prescaler+1) / (1+TS1+TS2=6) = bps;
        /// 800K 行使用特殊时序(SJW=0,TS1=1,TS2=0), 与上式不一致, 原样保留参考值。
        /// </summary>
        internal static EbElFcbaseLibDLL.fcCANCcConfig BuildCcConfig(EbElCanBitrate bitrate)
        {
            var cfg = new EbElFcbaseLibDLL.fcCANCcConfig
            {
                SynchronizationJumpWidth = 1,
                TimeSegment1 = 4,
                TimeSegment2 = 1,
                // 参考实现遗留默认值 0(关闭自动重发=变相单次发送), 这里修正为使能
                EnableAutomaticRetransmission = 1,
            };
            cfg.BaudRatePrescaler = bitrate switch
            {
                EbElCanBitrate.fcBAUD_1M => 1,
                EbElCanBitrate.fcBAUD_500K => 3,
                EbElCanBitrate.fcBAUD_250K => 7,
                EbElCanBitrate.fcBAUD_125K => 15,
                EbElCanBitrate.fcBAUD_100K => 19,
                EbElCanBitrate.fcBAUD_50K => 39,
                EbElCanBitrate.fcBAUD_20K => 99,
                EbElCanBitrate.fcBAUD_10K => 199,
                EbElCanBitrate.fcBAUD_800K => 4,
                _ => throw new ArgumentException($"Unsupported bitrate({bitrate})."),
            };
            if (bitrate == EbElCanBitrate.fcBAUD_800K)
            {
                cfg.SynchronizationJumpWidth = 0;
                cfg.TimeSegment1 = 1;
                cfg.TimeSegment2 = 0;
            }
            return cfg;
        }

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

                // 先停监控再关句柄
                if (_handle != IntPtr.Zero)
                {
                    try { EbElFcbaseLibDLL.MonitoringStop(_handle, _cc); } catch { }
                    try { EbElFcbaseLibDLL.Close(_handle); } catch { }
                    _handle = IntPtr.Zero;
                }
                _isOpen = false;
            }
            return true;
        }

        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                // fcbase 无专用清空函数,循环取空驱动接收队列即可(取到即弃)
                if (_handle == IntPtr.Zero) return;
                while (true)
                {
                    var result = EbElFcbaseLibDLL.Receive(_handle, out var pPacket);
                    if (result != EbElFcbaseLibDLL.fcResult.OK || pPacket == IntPtr.Zero)
                        break;
                    EbElFcbaseLibDLL.FreePacket(pPacket);
                }
            }
        }

        public void FlushTransmitBuffer()
        {
            // fcbase 发送直接交付报文缓冲,无用户态发送队列可清空
        }

        private void ReadTaskProc(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var handle = _handle;
                    if (handle == IntPtr.Zero) break;

                    if (!ReceiveChain(handle))
                    {
                        // 无报文,小睡避免空转
                        Thread.Sleep(RxPollIdleMs);
                    }
                }
                catch (Exception)
                {
                    // 其他异常，稍等后继续
                    try { if (!token.IsCancellationRequested) Thread.Sleep(10); }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 取一串报文包并处理; 返回是否取到报文。
        /// 关键纪律: 先在 finally 之前把整条链表编组遍历完, 再 fcFreeMemory 归还
        /// (参考实现先释放后访问, 且循环条件写反, 整条接收路径从未真正工作过)。
        /// </summary>
        private bool ReceiveChain(IntPtr handle)
        {
            var result = EbElFcbaseLibDLL.Receive(handle, out var pPacket);
            if (result != EbElFcbaseLibDLL.fcResult.OK || pPacket == IntPtr.Zero)
                return false;

            try
            {
                var pCurrent = pPacket;
                while (pCurrent != IntPtr.Zero)
                {
                    // 逐节点编组(参考实现只编组了首节点)
                    var packet = Marshal.PtrToStructure<EbElFcbaseLibDLL.fcPacket>(pCurrent);
                    if (packet.Type == EbElFcbaseLibDLL.fcPacketType.CAN && packet.pCANPacket != IntPtr.Zero)
                    {
                        var canPacket = Marshal.PtrToStructure<EbElFcbaseLibDLL.fcCANPacket>(packet.pCANPacket);
                        ProcessCanPacket(in canPacket);
                    }
                    pCurrent = packet.pNextPacket;
                }
            }
            finally
            {
                EbElFcbaseLibDLL.FreePacket(pPacket);
            }
            return true;
        }

        private void ProcessCanPacket(in EbElFcbaseLibDLL.fcCANPacket canPacket)
        {
            // 过滤: 非本通道 / 发送确认(由 Send 成功后自行推送)
            if (canPacket.CC != _cc) return;
            byte flagsAndDlc = (byte)canPacket.FlagsAndDLC;
            if ((flagsAndDlc & (byte)EbElFcbaseLibDLL.DLCFlags.Direction) != 0) return;

            // bit0-3 直接是字节数(0..8), 钳制防御
            int length = Math.Min(flagsAndDlc & 0x0F, DataLenConverter.MaxCanDataLength);
            byte[] data = new byte[length];
            Array.Copy(canPacket.Data, 0, data, 0, length);

            CANFrame frame1 = new CANFrame();
            frame1.ID = BuildManagedId(canPacket.ID, flagsAndDlc);
            frame1.DLC = length;
            frame1.Flag = flagsAndDlc;
            frame1.Flags = EbElCanFlagParser.Parse(canPacket.ID, flagsAndDlc);
            // 时间戳单位未公开, 原始 tick 透传(与 Kvaser 毫秒直给同款"未换算")
            frame1.Timestamp = canPacket.TimeStamp;
            frame1.Data = data;
            frame1.Dir = 1;

            eventHandler?.Invoke(this, frame1);
        }

        // 托管层 SocketCAN 高位标志(bit31=EFF, bit30=RTR) -> fcbase 原生 ID(bit29=EFF, RTR 在 FlagsAndDLC)
        internal static uint BuildFlexCardId(long id)
        {
            uint result = CanFrameHelper.GetId(id);
            if (CanFrameHelper.IsEFF(id)) result |= EbElFcbaseLibDLL.FC_EXT_FLAG;
            return result;
        }

        // fcbase 原生 ID + FlagsAndDLC -> 合成 SocketCAN 高位标志的托管 ID
        internal static long BuildManagedId(uint fcId, byte flagsAndDlc)
        {
            long id = fcId & CanFrameDef.CAN_ID_FLAG;
            if ((fcId & EbElFcbaseLibDLL.FC_EXT_FLAG) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
            if ((flagsAndDlc & (byte)EbElFcbaseLibDLL.DLCFlags.RemoteFrame) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
            return id;
        }

        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _handle == IntPtr.Zero) return false;

                if (frame is not CANFrame canFrame) return false;

                //发送前校验数据长度合法性（非法参数直接抛异常，不做静默失败）
                if (canFrame.FDF || canFrame.BRS)
                    throw new ArgumentException("EbEl(FlexCard) channel does not support CANFD frame(FDF/BRS).");
                if (CanFrameHelper.IsRTR(canFrame.ID))
                    throw new ArgumentException("EbEl(FlexCard) buffer-based transmit does not support RTR.");
                if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
                    throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");
                if (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC)
                    throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

                try
                {
                    canFrame.Dir = 0;

                    // 缓冲式发送: fcbCANTransmit 只收缓冲号 + 数据, 不带 CAN ID
                    // —— 报文的 ID/DLC 由缓冲 0 的既有配置决定, canFrame.ID 无法生效(见文档第六章)
                    byte[] data = new byte[canFrame.DLC];
                    Array.Copy(canFrame.Data, 0, data, 0, canFrame.DLC);
                    var result = EbElFcbaseLibDLL.Transmit(_handle, _cc, 0, data);
                    if (result != EbElFcbaseLibDLL.fcResult.OK) return false;

                    // 发送成功后回显(发送侧不还原缓冲 ID,原样回显上层给的帧)
                    uint rawId = BuildFlexCardId(canFrame.ID);
                    canFrame.Flag = (byte)EbElFcbaseLibDLL.DLCFlags.Direction;
                    canFrame.Flags = EbElCanFlagParser.Parse(rawId, (byte)EbElFcbaseLibDLL.DLCFlags.Direction);
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
