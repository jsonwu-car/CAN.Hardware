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
    /// - Master:Send 发送完整帧(报头+响应),RequestMessage 按指定 ID 发送报头,应答由后台任务经事件上抛;
    /// - Slave:Send 更新本节点应答数据(linUpdateMessage),收到匹配报头时由驱动发出;
    /// - Monitor:以 slave 打开但永不更新应答数据,只收不发。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Kvaser, KvaserDeviceType.Generic, CommProtocol.LIN)]
    public class KvaserLinWrapper : ILinHardware
    {
        private const int LinMaxDlc = 8;
        private const long LinMaxId = 0x3F;

        private int _handle = -1;
        private KvaserLinConfig _kvaserLinConfig = new KvaserLinConfig();
        private Linlib.LinStatus _linStatus;

        // 用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; } = null;

        // 用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _kvaserLinConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();

        // Task控制相关
        private bool _isOpen = false;
        private bool _closing = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        public object GetConfig() { return _kvaserLinConfig; }

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
                if (_kvaserLinConfig == null) return false;

                int tempHandle = -1;
                bool libraryLoaded = false;
                bool busOn = false;
                try
                {
                    KvaserLinLibDLL.InitializeLibrary();
                    libraryLoaded = true;

                    tempHandle = KvaserLinLibDLL.linOpenChannel((int)_kvaserLinConfig.DeviceChannel, MapOpenFlag(_kvaserLinConfig.NodeMode));
                    if (tempHandle < 0) throw new Exception($"open channel failed({((Linlib.LinStatus)tempHandle).GetEnumDescription()})");

                    _linStatus = KvaserLinLibDLL.linSetBitrate(tempHandle, _kvaserLinConfig.LinBitrate);
                    if (_linStatus != Linlib.LinStatus.linOK) throw new Exception($"Set lin bitrate failed({_linStatus.GetEnumDescription()})");

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
                // 即只收不发,WriteLocked 中已阻止 Monitor 主动发送
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
                        eventHandler?.Invoke(this, frame!);
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

        public bool RequestMessage(long linId)
        {
            if (linId < 0 || linId > LinMaxId) return false;
            lock (_lock)
            {
                if (!_isOpen || _handle < 0) return false;
                if (_kvaserLinConfig.NodeMode != LinNodeMode.Master) return false;
                try
                {
                    _linStatus = KvaserLinLibDLL.linRequestMessage(_handle, (uint)linId);
                    if (_linStatus == Linlib.LinStatus.linOK)
                    {
                        var frame = new LINFrame() { ID = linId, Dir = 0, Flag = (int)Linlib.LIN_TX, Flags = KvaserLinFlagParser.Parse((uint)Linlib.LIN_TX) };
                        frame.Flags = KvaserLinFlagParser.Parse((uint)frame.Flag);
                        eventHandler?.Invoke(this, frame);
                    }
                    return _linStatus == Linlib.LinStatus.linOK;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(RequestMessage)}:{ex.Message}");
                }
            }
        }

        public bool SetChecksumType(bool enhanced)
        {
            lock (_lock)
            {
                _kvaserLinConfig.ChecksumType = enhanced;
                if (!_isOpen || _handle < 0) return true;
                try
                {
                    _linStatus = KvaserLinLibDLL.linSetupLIN(_handle, (uint)((enhanced ? Linlib.LIN_ENHANCED_CHECKSUM : 0) | (_kvaserLinConfig.VariableDlc ? Linlib.LIN_VARIABLE_DLC : 0)), _kvaserLinConfig.LinBitrate);
                    return _linStatus == Linlib.LinStatus.linOK;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(SetChecksumType)}:{ex.Message}");
                }
            }
        }

        public bool WakeUp()
        {
            lock (_lock)
            {
                if (!_isOpen || _handle < 0) return false;
                if (_kvaserLinConfig.NodeMode != LinNodeMode.Master) return false;
                try
                {
                    _linStatus = KvaserLinLibDLL.linWriteWakeup(_handle, 1, 0);
                    if (_linStatus == Linlib.LinStatus.linOK)
                    {
                        var frame = new LINFrame() { ID = 0, Dir = 0, Flag = (int)Linlib.LIN_TX, Flags = KvaserLinFlagParser.Parse((uint)Linlib.LIN_TX) };
                        frame.Flags = KvaserLinFlagParser.Parse((uint)frame.Flag);
                        eventHandler?.Invoke(this, frame);
                    }
                    return _linStatus == Linlib.LinStatus.linOK;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(WakeUp)}:{ex.Message}");
                }
            }
        }

        public bool Send(FrameBase frame)
        {
            LINFrame? linFrame = frame as LINFrame;
            if (linFrame == null || linFrame.Data == null) return false;
            if (linFrame.ID < 0 || linFrame.ID > LinMaxId) return false;
            if (linFrame.DLC < 1 || linFrame.DLC > LinMaxDlc || linFrame.DLC != linFrame.Data.Length) return false;

            lock (_lock)
            {
                if (!_isOpen || _handle < 0) return false;
                try
                {
                    if (!WriteLocked(linFrame)) return false;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
            linFrame.Flag = (int)Linlib.LIN_TX;
            linFrame.Flags = KvaserLinFlagParser.Parse((uint)linFrame.Flag);
            eventHandler?.Invoke(this, linFrame);
            return true;
        }

        private unsafe bool WriteLocked(LINFrame linFrame)
        {
            if (_kvaserLinConfig.NodeMode == LinNodeMode.Master)
            {
                // 主节点:发送完整帧(报头+响应),报文即刻上线
                fixed (byte* ptr = linFrame.Data)
                {
                    _linStatus = KvaserLinLibDLL.linWriteMessage(_handle, (uint)linFrame.ID, (IntPtr)ptr, (uint)linFrame.Data.Length);
                }
            }
            else if (_kvaserLinConfig.NodeMode == LinNodeMode.Slave)
            {
                // 从节点:更新本节点该 ID 的应答数据,待主节点报头到达时由驱动发出
                fixed (byte* ptr = linFrame.Data)
                {
                    _linStatus = KvaserLinLibDLL.linUpdateMessage(_handle, (uint)linFrame.ID, (IntPtr)ptr, (uint)linFrame.Data.Length);
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
