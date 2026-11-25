using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;
using HLINCLIENT = System.Byte;
using HLINHW = System.UInt16;

namespace CAN.Hardware.Entity
{
    /// <summary>
    /// PEAK LIN(PLinApi.dll)封装。
    /// 三种节点模式的语义:
    /// - Master:Send 配置帧表并发送完整帧(报头+响应),RequestMessage 只发报头,应答由后台任务经事件上抛;
    /// - Slave:Send 配置帧表并更新应答数据,收到匹配报头时由设备自动应答;
    /// - Monitor:PLIN API 没有独立监听模式,Open 时抛异常。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Peak, PeakDeviceType.Generic, CommProtocol.LIN)]
    public class PeakLinWrapper : ILinHardware
    {
        private const int LinMaxDlc = 8;
        private const long LinMaxId = 0x3F;
        private const int MinBitrate = 1000;
        private const int MaxBitrate = 20000;
        private const ushort FrameFlagResponseEnable = 1;
        private const byte MasterWakeUpFrameId = 0x3D;

        private readonly PeakLinConfig _peakLinConfig = new PeakLinConfig();
        private TLINError _linError;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; } = null;

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _peakLinConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();

        // Task控制相关
        private bool _isOpen = false;
        private bool _closing = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        private HLINCLIENT _client = 0;
        private HLINHW _hw = 0;
        private readonly Dictionary<byte, FrameEntryState> _frameEntryCache = new Dictionary<byte, FrameEntryState>();

        private struct FrameEntryState
        {
            public TLINDirection Direction;
            public byte Length;
        }

        public object GetConfig() { return _peakLinConfig; }

        public bool Close()
        {
            Task? taskToWait;
            CancellationTokenSource? ctsToCancel;
            HLINCLIENT clientToCleanup;
            HLINHW hwToCleanup;

            lock (_lock)
            {
                if (!_isOpen) return true;
                _isOpen = false;
                _closing = true;
                clientToCleanup = _client;
                hwToCleanup = _hw;
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
                if (IsSoleClientLocked(hwToCleanup, clientToCleanup))
                {
                    try { PeakLinLibDLL.ResetHardwareConfig(clientToCleanup, hwToCleanup); } catch { }
                }
                try { PeakLinLibDLL.DisconnectClient(clientToCleanup, hwToCleanup); } catch { }
                try { PeakLinLibDLL.RemoveClient(clientToCleanup); } catch { }
                _client = 0;
                _hw = 0;
                _frameEntryCache.Clear();
                if (ctsToCancel != null)
                {
                    try { ctsToCancel.Dispose(); }
                    catch (Exception) { }
                }
                _closing = false;
            }
            return true;
        }

        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (!_isOpen) return;
                try { _linError = PeakLinLibDLL.ResetClient(_client); } catch { }
            }
        }

        // PLIN 的 LIN_ResetHardware 同时清空硬件收发队列，作为发送缓冲清理使用
        public void FlushTransmitBuffer()
        {
            lock (_lock)
            {
                if (!_isOpen) return;
                try { _linError = PeakLinLibDLL.ResetHardware(_client, _hw); } catch { }
            }
        }

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;
                if (_closing) return false;
                if (!Enum.IsDefined(typeof(LinNodeMode), _peakLinConfig.NodeMode) || _peakLinConfig.NodeMode == LinNodeMode.Monitor)
                    throw new Exception($"Unsupported node type({_peakLinConfig.NodeMode})");
                if (_peakLinConfig.LinBitrate < MinBitrate || _peakLinConfig.LinBitrate > MaxBitrate)
                    throw new Exception($"Unsupported bitrate({_peakLinConfig.LinBitrate})");

                HLINCLIENT client = 0;
                bool clientRegistered = false;
                HLINHW hw = 0;
                bool clientConnected = false;
                try
                {
                    _linError = PeakLinLibDLL.RegisterClient("CAN.Hardware.PeakLin", IntPtr.Zero, out client);
                    if (_linError != TLINError.errOK) throw new Exception($"Register client failed({_linError.GetEnumDescription()})");
                    clientRegistered = true;

                    // 先查数量,再按 HardwareIndex 取句柄
                    _linError = PeakLinLibDLL.GetAvailableHardware(null!, 0, out ushort hwCount);
                    if (_linError != TLINError.errOK) throw new Exception($"Get hardware failed({_linError.GetEnumDescription()})");
                    if (hwCount == 0) throw new Exception("No LIN hardware found");
                    if (_peakLinConfig.HardwareIndex >= hwCount) throw new Exception($"Hardware index out of range(index:{_peakLinConfig.HardwareIndex}, count:{hwCount})");

                    HLINHW[] handles = new HLINHW[hwCount];
                    _linError = PeakLinLibDLL.GetAvailableHardware(handles, (ushort)(handles.Length * 2), out _);
                    if (_linError != TLINError.errOK) throw new Exception($"Get hardware failed({_linError.GetEnumDescription()})");
                    hw = handles[(int)_peakLinConfig.HardwareIndex];

                    _linError = PeakLinLibDLL.ConnectClient(client, hw);
                    if (_linError != TLINError.errOK) throw new Exception($"Connect hardware failed({_linError.GetEnumDescription()})");
                    clientConnected = true;

                    _linError = PeakLinLibDLL.InitializeHardware(client, hw, _peakLinConfig.NodeMode == LinNodeMode.Master ? TLINHardwareMode.modMaster : TLINHardwareMode.modSlave, (ushort)_peakLinConfig.LinBitrate);
                    if (_linError != TLINError.errOK) throw new Exception($"Initialize hardware failed({_linError.GetEnumDescription()})");

                    // 过滤掩码全开;状态帧上报在随后的 SetClientParam 中关闭
                    _linError = PeakLinLibDLL.SetClientFilter(client, hw, ulong.MaxValue);
                    if (_linError != TLINError.errOK) throw new Exception($"Set client filter failed({_linError.GetEnumDescription()})");

                    // 参考实现关闭状态帧上报(参数 0 = deactivated)
                    _linError = PeakLinLibDLL.SetClientParam(client, TLINClientParam.clpReceiveStatusFrames, 0);
                    if (_linError != TLINError.errOK) throw new Exception($"Set client param failed({_linError.GetEnumDescription()})");

                    _linError = PeakLinLibDLL.ResetClient(client);
                    if (_linError != TLINError.errOK) throw new Exception($"Reset client failed({_linError.GetEnumDescription()})");

                    // 所有操作成功后，才提交状态
                    _client = client;
                    _hw = hw;
                    _frameEntryCache.Clear();
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (clientConnected) { try { PeakLinLibDLL.DisconnectClient(client, hw); } catch { } }
                    if (clientRegistered) { try { PeakLinLibDLL.RemoveClient(client); } catch { } }
                    if (_cts != null) { try { _cts.Dispose(); } catch { } _cts = null; }
                    _readTask = null;
                    _isOpen = false;
                    _frameEntryCache.Clear();
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        private bool IsSoleClientLocked(HLINHW hw, HLINCLIENT client)
        {
            try
            {
                byte[] connected = new byte[64];
                TLINError error = PeakLinLibDLL.GetHardwareParam(hw, TLINHardwareParam.hwpConnectedClients, connected, (ushort)connected.Length);
                if (error != TLINError.errOK || connected.Length < 2) return false;
                if (connected[0] != 1) return false;
                return connected[1] == client;
            }
            catch { return false; }
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
            if (linId < 0 || linId > LinMaxId)
                throw new Exception($"{nameof(RequestMessage)}:Invalid frame id({linId})");
            lock (_lock)
            {
                if (!_isOpen)
                    throw new Exception($"{nameof(RequestMessage)}:LIN channel is not open");
                if (_peakLinConfig.NodeMode != LinNodeMode.Master)
                    throw new Exception($"{nameof(RequestMessage)}:Not in master mode({_peakLinConfig.NodeMode})");
                try
                {
                    byte frameId = (byte)linId;
                    if (!EnsureFrameEntryLocked(frameId, TLINDirection.dirSubscriberAutoLength, LinMaxDlc, null))
                        throw new Exception($"Set frame entry failed(id:{frameId}, error:{_linError.GetEnumDescription()})");

                    // TLINMsg.FrameId 需要带奇偶位的 PID
                    byte pid = frameId;
                    _linError = PeakLinLibDLL.GetPID(ref pid);
                    if (_linError != TLINError.errOK)
                        throw new Exception($"Get PID failed({_linError.GetEnumDescription()})");

                    TLINMsg msg = new TLINMsg();
                    msg.FrameId = pid;
                    msg.Length = LinMaxDlc;
                    msg.Direction = TLINDirection.dirSubscriberAutoLength;
                    msg.ChecksumType = TLINChecksumType.cstAuto;
                    msg.Data = new byte[LinMaxDlc];
                    msg.Checksum = 0;
                    _linError = PeakLinLibDLL.Write(_client, _hw, ref msg);
                    if (_linError != TLINError.errOK)
                        throw new Exception($"Request message failed({_linError.GetEnumDescription()})");
                    return true;
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
                _peakLinConfig.ChecksumType = enhanced;
                // 帧条目缓存不含校验类型,需清空以便下次发送按新校验类型重建
                _frameEntryCache.Clear();
                return true;
            }
        }

        public bool SetIllegalMessage(long linId, bool checksumError, bool parityError, uint delayBitTimes)
        {
            throw new NotSupportedException($"{nameof(SetIllegalMessage)}:PEAK LIN API 当前未暴露等价的错误注入能力");
        }

        public bool ClearMessage(long linId)
        {
            throw new NotSupportedException($"{nameof(ClearMessage)}:PEAK LIN API 当前未暴露等价的从节点缓冲清除能力");
        }

        public bool WakeUp()
        {
            lock (_lock)
            {
                if (!_isOpen)
                    throw new Exception($"{nameof(WakeUp)}:LIN channel is not open");
                try
                {
                    if (_peakLinConfig.NodeMode == LinNodeMode.Master)
                    {
                        // PLIN Master 通过发送一帧唤醒总线;0x3D 订阅帧不会干扰正常业务数据
                        byte pid = MasterWakeUpFrameId;
                        _linError = PeakLinLibDLL.GetPID(ref pid);
                        if (_linError != TLINError.errOK)
                            throw new Exception($"Get PID failed({_linError.GetEnumDescription()})");

                        TLINMsg msg = new TLINMsg();
                        msg.FrameId = pid;
                        msg.Length = LinMaxDlc;
                        msg.Direction = TLINDirection.dirSubscriberAutoLength;
                        msg.ChecksumType = TLINChecksumType.cstAuto;
                        msg.Data = new byte[LinMaxDlc] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                        msg.Checksum = 0;
                        _linError = PeakLinLibDLL.Write(_client, _hw, ref msg);
                    }
                    else if (_peakLinConfig.NodeMode == LinNodeMode.Slave)
                    {
                        // PLIN Slave 使用 LIN_XmtWakeUp 发送唤醒脉冲
                        _linError = PeakLinLibDLL.XmtWakeUp(_client, _hw);
                    }
                    else
                    {
                        throw new Exception($"Unsupported node type({_peakLinConfig.NodeMode})");
                    }

                    if (_linError != TLINError.errOK)
                        throw new Exception($"Wake up failed({_linError.GetEnumDescription()})");
                    return true;
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
            if (linFrame == null || linFrame.Data == null)
                throw new Exception($"{nameof(Send)}:Frame is null or data is null");
            if (linFrame.ID < 0 || linFrame.ID > LinMaxId)
                throw new Exception($"{nameof(Send)}:Invalid frame id({linFrame.ID})");
            if (linFrame.DLC < 1 || linFrame.DLC > LinMaxDlc || linFrame.DLC != linFrame.Data.Length)
                throw new Exception($"{nameof(Send)}:Invalid frame length({linFrame.DLC})");

            lock (_lock)
            {
                if (!_isOpen)
                    throw new Exception($"{nameof(Send)}:LIN channel is not open");

                bool result;
                try
                {
                    result = WriteLocked(linFrame);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
                if (!result)
                    throw new Exception($"{nameof(Send)}:Write frame failed");
                return result;
            }
        }

        private bool WriteLocked(LINFrame linFrame)
        {
            if (_peakLinConfig.NodeMode == LinNodeMode.Monitor) return false;

            byte frameId = (byte)linFrame.ID;
            byte length = (byte)linFrame.DLC;
            if (!EnsureFrameEntryLocked(frameId, TLINDirection.dirPublisher, length, linFrame.Data)) return false;

            if (_peakLinConfig.NodeMode == LinNodeMode.Master)
            {
                byte pid = frameId;
                _linError = PeakLinLibDLL.GetPID(ref pid);
                if (_linError != TLINError.errOK) return false;

                TLINMsg msg = new TLINMsg();
                msg.FrameId = pid;
                msg.Length = length;
                msg.Direction = TLINDirection.dirPublisher;
                msg.ChecksumType = _peakLinConfig.ChecksumType ? TLINChecksumType.cstEnhanced : TLINChecksumType.cstClassic;
                msg.Data = new byte[LinMaxDlc];
                Array.Copy(linFrame.Data, msg.Data, length);
                msg.Checksum = 0;
                _linError = PeakLinLibDLL.Write(_client, _hw, ref msg);
                if (_linError != TLINError.errOK) return false;
            }
            else
            {
                _linError = PeakLinLibDLL.UpdateByteArray(_client, _hw, frameId, 0, length, linFrame.Data);
                if (_linError != TLINError.errOK) return false;
            }
            linFrame.Dir = 0;
            return true;
        }

        private bool TryReadFrameLocked(out LINFrame? frame)
        {
            frame = null;
            if (!_isOpen) return false;

            while (true)
            {
                _linError = PeakLinLibDLL.Read(_client, out TLINRcvMsg rcv);
                if (_linError != TLINError.errOK) return false;
                if (rcv.Type == TLINMsgType.mstStandard && rcv.Length > LinMaxDlc) continue;
                frame = BuildFrame(rcv);
                return true;
            }
        }

        private bool EnsureFrameEntryLocked(byte frameId, TLINDirection direction, byte length, byte[]? initialData)
        {
            if (_frameEntryCache.TryGetValue(frameId, out FrameEntryState state) && state.Direction == direction && state.Length == length) return true;

            TLINFrameEntry entry = new TLINFrameEntry();
            entry.FrameId = frameId;
            entry.Length = length;
            entry.Direction = direction;
            entry.ChecksumType = direction == TLINDirection.dirPublisher
                ? (_peakLinConfig.ChecksumType ? TLINChecksumType.cstEnhanced : TLINChecksumType.cstClassic)
                : TLINChecksumType.cstAuto;
            entry.Flags = FrameFlagResponseEnable;
            entry.InitialData = new byte[LinMaxDlc];
            if (initialData != null) Array.Copy(initialData, entry.InitialData, Math.Min(initialData.Length, LinMaxDlc));

            _linError = PeakLinLibDLL.SetFrameEntry(_client, _hw, ref entry);
            if (_linError != TLINError.errOK) return false;

            _frameEntryCache[frameId] = new FrameEntryState { Direction = direction, Length = length };
            return true;
        }

        private static LINFrame BuildFrame(TLINRcvMsg rcv)
        {
            LINFrame frame = new LINFrame();
            frame.ID = rcv.FrameId & 0x3F;
            frame.DLC = rcv.Length;
            frame.Data = new byte[rcv.Length];
            if (rcv.Data != null) Array.Copy(rcv.Data, frame.Data, Math.Min(rcv.Length, rcv.Data.Length));
            frame.Flag = (int)rcv.ErrorFlags;
            frame.Flags = PeakLinFlagParser.Parse(rcv);
            frame.Timestamp = rcv.TimeStamp;
            frame.Dir = rcv.Direction == TLINDirection.dirPublisher ? 0 : 1;
            return frame;
        }
    }
}
