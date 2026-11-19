using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    /// <summary>
    /// 同星(Tosun) LIN(libTSCAN)封装。
    /// 三种节点模式的语义:
    /// - Master:Send 发送完整帧(报头+响应),RequestMessage 按指定 ID 发送报头,应答由后台任务经事件上抛;
    /// - Slave:Send 更新本节点应答数据并重建调度表,收到匹配报头时由驱动发出;
    /// - Monitor:只收不发。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Tosun, TosunDeviceType.Generic, CommProtocol.LIN)]
    public class ToSunLinWrapper : ILinHardware
    {
        private const int LinMaxDlc = 8;
        private const long LinMaxId = 0x3F;
        private const int ReceiveBatchSize = 16;
        private const int MinBitrate = 1000;
        private const int MaxBitrate = 20000;
        private const byte SlaveScheduleDelayMs = 10;

        private readonly ToSunLINConfig _linConfig = new ToSunLINConfig();
        private uint _status = 0;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _linConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();

        // Task控制相关
        private bool _isOpen = false;
        private bool _closing = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        //同星设备句柄,IntPtr.Zero 表示未打开
        private IntPtr _handle = IntPtr.Zero;
        private TLIBLIN[] _receiveBuffer = new TLIBLIN[ReceiveBatchSize];
        private readonly Dictionary<byte, TLIBLIN> _slaveScheduleFrames = new Dictionary<byte, TLIBLIN>();

        public object GetConfig() { return _linConfig; }

        public bool Close()
        {
            Task? taskToWait;
            CancellationTokenSource? ctsToCancel;
            IntPtr handleToClose;

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
                if (handleToClose != IntPtr.Zero)
                {
                    try { _status = ToSunCanLibDLL.tslin_stop_lin_channel(handleToClose, _linConfig.ChannelIndex); } catch { }
                }
                try { ToSunCanLibDLL.DisconnectDevice(); } catch { }
                if (_handle == handleToClose) _handle = IntPtr.Zero;
                _slaveScheduleFrames.Clear();
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
                if (!_isOpen || _handle == IntPtr.Zero) return;
                try { _status = ToSunCanLibDLL.tsfifo_clear_lin_receive_buffers(_handle, _linConfig.ChannelIndex); } catch { }
            }
        }

        public void FlushTransmitBuffer()
        {
            // libTSCAN 没有清除发送缓冲区的 API,空操作
        }

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;
                if (_closing) return false;
                if (!Enum.IsDefined(typeof(LinNodeMode), _linConfig.NodeMode))
                    throw new Exception($"Unsupported node type({_linConfig.NodeMode})");
                if (_linConfig.LinBitrate < MinBitrate || _linConfig.LinBitrate > MaxBitrate)
                    throw new Exception($"Unsupported bitrate({_linConfig.LinBitrate})");

                IntPtr handle = IntPtr.Zero;
                bool connected = false;
                try
                {
                    // 连接设备(内部完成库初始化,多实例共享设备句柄)
                    _status = ToSunCanLibDLL.ConnectDevice(null, _linConfig.EnableFIFO, _linConfig.EnableErrorFrame, _linConfig.EnableTurbe, out handle);
                    if (_status != 0) throw new Exception($"Connect device failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");
                    connected = true;

                    // 设置 LIN 波特率与协议版本(协议版本决定经典/增强校验)
                    _status = ToSunCanLibDLL.tslin_config_baudrate(handle, _linConfig.ChannelIndex, _linConfig.LinBitrate / 1000.0, _linConfig.LinProtocol);
                    if (_status != 0) throw new Exception($"Config lin baudrate failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

                    // 设置节点角色(主/从/监听)
                    _status = ToSunCanLibDLL.tslin_set_node_functiontype(handle, _linConfig.ChannelIndex, MapNodeFunction(_linConfig.NodeMode));
                    if (_status != 0) throw new Exception($"Set lin node type failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

                    // 启动 LIN 通道
                    _status = ToSunCanLibDLL.tslin_start_lin_channel(handle, _linConfig.ChannelIndex);
                    if (_status != 0) throw new Exception($"Start lin channel failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

                    // 所有操作成功后，才提交状态
                    _handle = handle;
                    _slaveScheduleFrames.Clear();
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (connected) { try { ToSunCanLibDLL.DisconnectDevice(); } catch { } }
                    if (_cts != null) { try { _cts.Dispose(); } catch { } _cts = null; }
                    _readTask = null;
                    _isOpen = false;
                    _handle = IntPtr.Zero;
                    _slaveScheduleFrames.Clear();
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        private static T_LIN_NODE_FUNCTION MapNodeFunction(LinNodeMode nodeMode)
        {
            switch (nodeMode)
            {
                case LinNodeMode.Master: return T_LIN_NODE_FUNCTION.T_MASTER_NODE;
                case LinNodeMode.Slave: return T_LIN_NODE_FUNCTION.T_SLAVE_NODE;
                case LinNodeMode.Monitor: return T_LIN_NODE_FUNCTION.T_MONITOR_NODE;
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
            if (linId < 0 || linId > LinMaxId)
                throw new Exception($"{nameof(RequestMessage)}:Invalid frame id({linId})");
            lock (_lock)
            {
                if (!_isOpen || _handle == IntPtr.Zero)
                    throw new Exception($"{nameof(RequestMessage)}:LIN channel is not open");
                if (_linConfig.NodeMode != LinNodeMode.Master) return false;
                try
                {
                    // 主节点只发报头,从节点应答由后台接收任务异步上抛
                    TLIBLIN request = new TLIBLIN(_linConfig.ChannelIndex, (byte)linId, (byte)LinMaxDlc, false);
                    _status = ToSunCanLibDLL.tslin_transmit_lin_async(_handle, ref request);
                    return _status == 0;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(RequestMessage)}:{ex.Message}");
                }
            }
        }

        // ToSun 没有独立的校验和类型 API:校验方式由 LIN 协议版本决定,
        // LIN 1.3/J2602 为经典校验,LIN 2.0/2.1 为增强校验。
        // 未打开时更新协议版本配置,打开后仅校验请求与当前配置是否一致。
        public bool SetChecksumType(bool enhanced)
        {
            lock (_lock)
            {
                bool currentEnhanced = IsEnhancedProtocol(_linConfig.LinProtocol);
                if (!_isOpen)
                {
                    if (currentEnhanced != enhanced)
                        _linConfig.LinProtocol = enhanced ? LIN_PROTOCOL.LIN_PROTOCOL_20 : LIN_PROTOCOL.LIN_PROTOCOL_13;
                    return true;
                }
                return currentEnhanced == enhanced;
            }
        }

        private static bool IsEnhancedProtocol(LIN_PROTOCOL protocol)
        {
            return protocol == LIN_PROTOCOL.LIN_PROTOCOL_20 || protocol == LIN_PROTOCOL.LIN_PROTOCOL_21;
        }

        public bool SetIllegalMessage(long linId, bool checksumError, bool parityError, uint delayBitTimes)
        {
            throw new NotSupportedException($"{nameof(SetIllegalMessage)}:ToSun LIN API 当前未暴露等价的错误注入能力");
        }

        public bool ClearMessage(long linId)
        {
            if (linId < 0 || linId > LinMaxId)
                throw new Exception($"{nameof(ClearMessage)}:Invalid frame id({linId})");
            lock (_lock)
            {
                if (!_isOpen || _handle == IntPtr.Zero)
                    throw new Exception($"{nameof(ClearMessage)}:LIN channel is not open");
                if (_linConfig.NodeMode != LinNodeMode.Slave)
                    throw new Exception($"{nameof(ClearMessage)}:Not in slave mode({_linConfig.NodeMode})");
                try
                {
                    if (!_slaveScheduleFrames.Remove((byte)linId)) return true;
                    return RebuildSlaveScheduleLocked();
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(ClearMessage)}:{ex.Message}");
                }
            }
        }

        public bool WakeUp()
        {
            lock (_lock)
            {
                if (!_isOpen || _handle == IntPtr.Zero)
                    throw new Exception($"{nameof(WakeUp)}:LIN channel is not open");
                if (_linConfig.NodeMode != LinNodeMode.Master)
                    throw new Exception($"{nameof(WakeUp)}:Not in master mode({_linConfig.NodeMode})");
                try
                {
                    // ToSun LIN 无专用唤醒 API:发送一帧完整 TX 报文,其帧头 Break 场
                    // (≥13 bit 显性电平)即满足 ISO 17987 唤醒信号要求。
                    // 必须 IsTx=true 且 DLC>0,否则固件可能不产生总线波形。
                    // 用 0x3C(MasterReq)填充全 0 数据,从节点收到无效 SID 会忽略。
                    TLIBLIN wake = new TLIBLIN(_linConfig.ChannelIndex, 0x3C, (byte)LinMaxDlc, true, new byte[LinMaxDlc]);
                    _status = ToSunCanLibDLL.tslin_transmit_lin_async(_handle, ref wake);
                    if (_status != 0)
                        throw new Exception($"Wake up failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");
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
                if (!_isOpen || _handle == IntPtr.Zero)
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
            if (_linConfig.NodeMode == LinNodeMode.Monitor) return false;

            if (_linConfig.NodeMode == LinNodeMode.Slave)
            {
                // 从节点:保存应答数据并重建调度表,待主节点报头到达时由驱动发出
                TLIBLIN stored = new TLIBLIN(_linConfig.ChannelIndex, (byte)linFrame.ID, (byte)linFrame.DLC, true, linFrame.Data);
                _slaveScheduleFrames[(byte)linFrame.ID] = stored;
                return RebuildSlaveScheduleLocked();
            }
            else if (_linConfig.NodeMode == LinNodeMode.Master)
            {
                // 主节点:发送完整帧(报头+响应),报文即刻上线
                byte[] data = new byte[LinMaxDlc];
                Array.Copy(linFrame.Data, data, linFrame.DLC);
                TLIBLIN msg = new TLIBLIN(_linConfig.ChannelIndex, (byte)linFrame.ID, (byte)linFrame.DLC, true, data);
                msg.FChecksum = 0x14;
                _status = ToSunCanLibDLL.tslin_transmit_lin_async(_handle, ref msg);
                return _status == 0;
            }
            else
            {
                throw new NotSupportedException($"{nameof(WriteLocked)}:Unsupported node type({_linConfig.NodeMode})");
            }
        }

        private bool RebuildSlaveScheduleLocked()
        {
            int chn = (int)_linConfig.ChannelIndex;
            _status = ToSunCanLibDLL.tslin_batch_set_schedule_start(_handle, chn);
            if (_status != 0) throw new Exception($"Start schedule failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

            foreach (TLIBLIN stored in _slaveScheduleFrames.Values)
            {
                TLIBLIN entry = stored;
                _status = ToSunCanLibDLL.tslin_batch_add_schedule_frame(_handle, chn, ref entry, SlaveScheduleDelayMs);
                if (_status != 0) throw new Exception($"Add schedule frame failed(id:{stored.FIdentifier}, status:{ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");
            }

            _status = ToSunCanLibDLL.tslin_batch_set_schedule_end(_handle, chn);
            if (_status != 0) throw new Exception($"End schedule failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

            _status = ToSunCanLibDLL.tslin_switch_runtime_schedule_table(_handle, chn);
            if (_status != 0) throw new Exception($"Switch runtime schedule failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");
            return true;
        }

        private bool TryReadFrameLocked(out LINFrame? frame)
        {
            frame = null;
            if (!_isOpen || _handle == IntPtr.Zero) return false;

            while (true)
            {
                if (ToSunCanLibDLL.ReceiveLINMsgList(_handle, ref _receiveBuffer, 1, _linConfig.ChannelIndex, READ_TX_RX_DEF.TX_RX_MESSAGES) <= 0) return false;
                TLIBLIN msg = _receiveBuffer[0];
                if (msg.FDLC > LinMaxDlc) continue;
                frame = BuildFrame(msg);
                return true;
            }
        }

        private static LINFrame BuildFrame(TLIBLIN msg)
        {
            LINFrame frame = new LINFrame();
            frame.ID = msg.FIdentifier;
            frame.DLC = msg.FDLC;
            frame.Data = new byte[msg.FDLC];
            Array.Copy(msg.FData, frame.Data, msg.FDLC);
            frame.Flag = msg.FProperties;
            frame.Flags = ToSunLinFlagParser.Parse(msg);
            frame.Timestamp = msg.FTimeUS;
            frame.Dir = msg.FIsTx ? 0 : 1;
            return frame;
        }
    }
}
