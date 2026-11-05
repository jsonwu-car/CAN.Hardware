using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    /// <summary>
    /// Kvaser LIN(linlib.dll)封装。
    /// 三种节点模式的语义:
    /// - Master:Send 发送完整帧(报头+响应),Read 按指定 ID 发送报头并等待从机应答;
    /// - Slave:Send 更新本节点应答数据(linUpdateMessage),收到匹配报头时由驱动发出;
    /// - Monitor:以 slave 打开但永不更新应答数据,只收不发。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Kvaser, KvaserDeviceType.Generic, CommProtocol.LIN)]
    public class KvaserLinWrapper : IHardware
    {
        private const int LinMaxDlc = 8;
        private const long LinMaxId = 0x3F;
        // 单次请求的总等待预算(次数×RequestDelayTime)需覆盖慢响应;
        // 注意休眠的ECU会漏掉第一个报头本身,延长等待无效,需调用方重试
        private const int MasterReadWaitAttempts = 15;

        private int _handle = -1;
        private readonly KvaserLinConfig _kvaserLinConfig = new KvaserLinConfig();
        private Linlib.LinStatus _linStatus;

        // 用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        // 用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _kvaserLinConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();

        // Task控制相关
        private bool _isOpen = false;
        private bool _closing = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        public bool Close()
        {
            Task? taskToWait;
            CancellationTokenSource? ctsToCancel;
            int handleToClose;

            lock (_lock)
            {
                if (!_isOpen) return true;
                _isOpen = false;
                _closing = true;
                handleToClose = _handle;
                ctsToCancel = _cts;
                taskToWait = _readTask;
                _cts = null;
                _readTask = null;
                if (ctsToCancel != null) ctsToCancel.Cancel();
            }

            // 在锁外部等待Task完成,避免死锁
            if (taskToWait != null)
            {
                try { taskToWait.Wait(1000); }
                catch (AggregateException) { }
                catch (Exception) { }
            }

            lock (_lock)
            {
                try
                {
                    if (handleToClose >= 0)
                    {
                        KvaserLinLibDLL.linBusOff(handleToClose);
                        KvaserLinLibDLL.linClose(handleToClose);
                    }
                }
                catch { }
                if (_handle == handleToClose) _handle = -1;
                KvaserLinLibDLL.UnloadLibrary();
                if (ctsToCancel != null)
                {
                    try { ctsToCancel.Dispose(); }
                    catch (Exception) { }
                }
                _closing = false;
            }
            return true;
        }

        public unsafe void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (!_isOpen || _handle < 0) return;
                try
                {
                    // linlib 无专用清队列接口,逐帧读出直至 linERR_NOMSG
                    byte* ptr = stackalloc byte[LinMaxDlc];
                    while (true)
                    {
                        uint id, dlc, flags;
                        KvaserLinLibDLL.LinMessageInfo msgInfo = new KvaserLinLibDLL.LinMessageInfo();
                        _linStatus = KvaserLinLibDLL.linReadMessage(_handle, out id, (IntPtr)ptr, out dlc, out flags, ref msgInfo);
                        if (_linStatus != Linlib.LinStatus.linOK) break;
                    }
                }
                catch { }
            }
        }

        public void FlushTransmitBuffer()
        {
            // linlib 发送为立即交付,无可清的发送缓冲区,保持空实现
        }

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;
                if (_closing) return false;

                int tempHandle = -1;
                bool libraryLoaded = false;
                bool busOn = false;
                try
                {
                    KvaserLinLibDLL.InitializeLibrary();
                    libraryLoaded = true;

                    tempHandle = KvaserLinLibDLL.linOpenChannel((int)_kvaserLinConfig.DeviceChannel, MapOpenFlag(_kvaserLinConfig.NodeMode));
                    if (tempHandle < 0) throw new Exception($"Open channel failed({((Linlib.LinStatus)tempHandle).GetEnumDescription()})");

                    _linStatus = KvaserLinLibDLL.linSetBitrate(tempHandle, _kvaserLinConfig.LinBitrate);
                    if (_linStatus != Linlib.LinStatus.linOK) throw new Exception($"Set {_kvaserLinConfig.LinBitrate} bps failed({_linStatus.GetEnumDescription()})");

                    _linStatus = KvaserLinLibDLL.linBusOn(tempHandle);
                    if (_linStatus != Linlib.LinStatus.linOK) throw new Exception($"Bus on failed({_linStatus.GetEnumDescription()})");
                    busOn = true;

                    _linStatus = KvaserLinLibDLL.linSetupLIN(tempHandle, (uint)((_kvaserLinConfig.ChecksumType ? Linlib.LIN_ENHANCED_CHECKSUM : 0) | (_kvaserLinConfig.VariableDlc ? Linlib.LIN_VARIABLE_DLC : 0)), _kvaserLinConfig.LinBitrate);
                    if (_linStatus != Linlib.LinStatus.linOK) throw new Exception($"Setup lin failed({_linStatus.GetEnumDescription()})");

                    // 所有操作成功后，才提交状态
                    _handle = tempHandle;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (tempHandle >= 0)
                    {
                        if (busOn) { try { KvaserLinLibDLL.linBusOff(tempHandle); } catch { } }
                        try { KvaserLinLibDLL.linClose(tempHandle); } catch { }
                    }
                    if (libraryLoaded) KvaserLinLibDLL.UnloadLibrary();
                    if (_cts != null) { try { _cts.Dispose(); } catch { } _cts = null; }
                    _readTask = null;
                    _isOpen = false;
                    _handle = -1;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        private static int MapOpenFlag(LinNodeMode nodeMode)
        {
            switch (nodeMode)
            {
                case LinNodeMode.Master: return Linlib.LIN_MASTER;
                case LinNodeMode.Slave: return Linlib.LIN_SLAVE;
                // linlib 无独立监听模式,标准做法是以 slave 打开、永不调用 linUpdateMessage,
                // 即只收不发,SendLocked 中已阻止 Monitor 主动发送
                case LinNodeMode.Monitor: return Linlib.LIN_SLAVE;
                default: throw new Exception($"Unsupported node type({nodeMode})");
            }
        }

        private async Task ReadTaskProc(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    LINFrame? frame = null;
                    bool hasFrame;
                    lock (_lock)
                    {
                        hasFrame = _isOpen && TryReadFrameLocked(out frame);
                    }
                    if (hasFrame)
                    {
                        eventHandler?.Invoke(this, frame);
                        continue;
                    }
                    await Task.Delay(5, token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception)
                {
                    try { if (!token.IsCancellationRequested) await Task.Delay(10, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }

        /// <summary>
        /// 主动读取一帧:主节点按传入 ID 发送报头等待从机应答;从/监听节点直接读驱动队列。
        /// </summary>
        public bool Read(ref FrameBase? frame)
        {
            LINFrame? result = null;
            List<LINFrame>? deferred = null;
            bool found = false;

            lock (_lock)
            {
                if (!_isOpen || _handle < 0) { frame = null; return false; }
                try
                {
                    if (_kvaserLinConfig.NodeMode == LinNodeMode.Master)
                    {
                        long requestId = frame != null ? frame.ID : -1;
                        if (requestId >= 0 && requestId <= LinMaxId)
                        {
                            deferred = new List<LINFrame>();
                            found = ReadMasterLocked(requestId, deferred, out result);
                        }
                    }
                    else
                    {
                        found = TryReadFrameLocked(out result);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Read)}:{ex.Message}");
                }
            }

            // 即使未等到目标响应,也上抛等待期间收到的其它帧(如带校验错误标志的响应),便于定位问题
            if (deferred != null)
            {
                for (int i = 0; i < deferred.Count; i++) eventHandler?.Invoke(this, deferred[i]);
            }
            if (!found) return false;
            frame = result;
            eventHandler?.Invoke(this, result);
            return true;
        }

        public bool Send(FrameBase frame)
        {
            if (frame is not LINFrame linFrame) return false;

            // 发送前校验参数合法性(非法参数直接抛异常,不做静默失败)
            if (linFrame.ID < 0 || linFrame.ID > LinMaxId)
                throw new ArgumentException($"Invalid LIN ID(0x{linFrame.ID:X}), valid range: 0x00-0x{LinMaxId:X2}.");
            if (linFrame.DLC < 1 || linFrame.DLC > LinMaxDlc)
                throw new ArgumentException($"Invalid LIN DLC({linFrame.DLC}), valid range: 1-{LinMaxDlc}.");
            if (linFrame.Data == null || linFrame.Data.Length < linFrame.DLC)
                throw new ArgumentException($"Data length({linFrame.Data?.Length ?? 0}) is less than DLC({linFrame.DLC}).");

            lock (_lock)
            {
                if (!_isOpen || _handle < 0) return false;
                try
                {
                    if (!SendLocked(linFrame)) return false;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
            eventHandler?.Invoke(this, linFrame);
            return true;
        }

        private unsafe bool SendLocked(LINFrame linFrame)
        {
            if (_kvaserLinConfig.NodeMode == LinNodeMode.Master)
            {
                // 主节点:发送完整帧(报头+响应),报文即刻上线
                fixed (byte* ptr = linFrame.Data)
                {
                    _linStatus = KvaserLinLibDLL.linWriteMessage(_handle, (uint)linFrame.ID, (IntPtr)ptr, (uint)linFrame.DLC);
                }
            }
            else if (_kvaserLinConfig.NodeMode == LinNodeMode.Slave)
            {
                // 从节点:更新本节点该 ID 的应答数据,待主节点报头到达时由驱动发出
                fixed (byte* ptr = linFrame.Data)
                {
                    _linStatus = KvaserLinLibDLL.linUpdateMessage(_handle, (uint)linFrame.ID, (IntPtr)ptr, (uint)linFrame.DLC);
                }
            }
            else
            {
                // 监听模式只收不发
                return false;
            }
            if (_linStatus != Linlib.LinStatus.linOK) return false;
            linFrame.Dir = 0;
            return true;
        }

        private unsafe bool TryReadFrameLocked(out LINFrame? frame)
        {
            frame = null;
            if (!_isOpen || _handle < 0) return false;

            byte* ptr = stackalloc byte[LinMaxDlc];
            while (true)
            {
                uint id, dlc, flags;
                KvaserLinLibDLL.LinMessageInfo msgInfo = new KvaserLinLibDLL.LinMessageInfo();
                _linStatus = KvaserLinLibDLL.linReadMessage(_handle, out id, (IntPtr)ptr, out dlc, out flags, ref msgInfo);
                if (_linStatus != Linlib.LinStatus.linOK) return false;
                if ((flags & (uint)Linlib.LIN_TX) != 0) continue;   // 丢弃本机发送回显,发送成功由 Send 自行回显
                if (dlc == 0 || dlc > LinMaxDlc) continue;          // 无数据帧/异常长度,继续读
                frame = BuildFrame(id, dlc, flags, ptr, msgInfo);
                return true;
            }
        }

        private unsafe bool ReadMasterLocked(long requestId, List<LINFrame> deferred, out LINFrame? result)
        {
            result = null;
            _linStatus = KvaserLinLibDLL.linRequestMessage(_handle, (uint)requestId);
            Thread.Sleep(50);
            if (_linStatus != Linlib.LinStatus.linOK) return false;

            const uint linErrorFlags = (uint)(Linlib.LIN_NODATA | Linlib.LIN_CSUM_ERROR | Linlib.LIN_SYNCH_ERROR | Linlib.LIN_BIT_ERROR);
            uint waitMs = _kvaserLinConfig.RequestDelayTime == 0 ? 1u : _kvaserLinConfig.RequestDelayTime;
            byte* ptr = stackalloc byte[LinMaxDlc];
            for (int attempt = 0; attempt < MasterReadWaitAttempts; attempt++)
            {
                uint id, dlc, flags;
                KvaserLinLibDLL.LinMessageInfo msgInfo = new KvaserLinLibDLL.LinMessageInfo();
                _linStatus = KvaserLinLibDLL.linReadMessageWait(_handle, out id, (IntPtr)ptr, out dlc, out flags, ref msgInfo, waitMs);
                // 等待窗口内没收到帧(linERR_NOMSG/linERR_TIMEOUT)不算失败,继续重试直到次数耗尽
                if (_linStatus == Linlib.LinStatus.linERR_NOMSG || _linStatus == Linlib.LinStatus.linERR_TIMEOUT) continue;
                if (_linStatus != Linlib.LinStatus.linOK) return false;
                if (dlc > LinMaxDlc) continue;
                // flags 是 LIN_xxx 位的组合而非单值,必须按位判断;带错误位的响应不当作有效应答
                if (id == (uint)requestId && (flags & (uint)Linlib.LIN_RX) != 0 && (flags & linErrorFlags) == 0 && dlc > 0)
                {
                    result = BuildFrame(id, dlc, flags, ptr, msgInfo);
                    return true;
                }
                if ((flags & (uint)Linlib.LIN_TX) != 0 || dlc == 0) continue;
                deferred.Add(BuildFrame(id, dlc, flags, ptr, msgInfo));
            }
            return false;
        }

        private unsafe LINFrame BuildFrame(uint id, uint dlc, uint flags, byte* data, KvaserLinLibDLL.LinMessageInfo msgInfo)
        {
            LINFrame frame = new LINFrame();
            frame.ID = id;
            frame.DLC = (int)dlc;
            frame.Data = new byte[dlc];
            for (int i = 0; i < dlc; i++) frame.Data[i] = data[i];
            frame.Flag = (int)flags;
            frame.Flags = KvaserLinFlagParser.Parse(flags, msgInfo);
            frame.Timestamp = msgInfo.timestamp;
            frame.Dir = (flags & (uint)Linlib.LIN_TX) != 0 ? 0 : 1;
            return frame;
        }
    }
}
