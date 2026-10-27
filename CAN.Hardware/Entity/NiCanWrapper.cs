using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Ni, NiDeviceType.Generic, CommProtocol.CAN)]
    public class NiCanWrapper : IHardware
    {
        //NI-XNET 会话句柄(0 表示未打开),输入/输出流各一个
        private uint _inputSession = 0;
        private uint _outputSession = 0;
        private readonly NiCANConfig _canConfig = new NiCANConfig();

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

                uint inSession = 0, outSession = 0;
                try
                {
                    // 基于 :memory: 伪数据库创建帧输入/输出流会话
                    int status = NiXnetLibDLL.nxCreateSession(NiXnetLibDLL.DB_CAN, string.Empty, string.Empty, _canConfig.InterfaceName, NiXnetLibDLL.NX_MODE_FRAME_IN_STREAM, out inSession);
                    if (status != NiXnetLibDLL.NX_SUCCESS)
                        throw new Exception($"Create input session failed({NiXnetLibDLL.GetStatusString(status)})");

                    status = NiXnetLibDLL.nxCreateSession(NiXnetLibDLL.DB_CAN, string.Empty, string.Empty, _canConfig.InterfaceName, NiXnetLibDLL.NX_MODE_FRAME_OUT_STREAM, out outSession);
                    if (status != NiXnetLibDLL.NX_SUCCESS)
                        throw new Exception($"Create output session failed({NiXnetLibDLL.GetStatusString(status)})");

                    // 两个会话共享同一物理接口,波特率需分别设置;
                    // 自定义模式打包高级位时序值(tqNs=Prescale*25, TSEG1=Prop+Phase1, TSEG2=Phase2)
                    ulong baudrate;
                    if (_canConfig.EnableCustomBitrate)
                    {
                        if (_canConfig.Tq != 1 + _canConfig.Prop + _canConfig.Phase1 + _canConfig.Phase2)
                            throw new ArgumentException($"Invalid bit timing: Tq({_canConfig.Tq}) must equal 1 + Prop + Phase1 + Phase2({1 + _canConfig.Prop + _canConfig.Phase1 + _canConfig.Phase2}).");
                        baudrate = NiXnetLibDLL.PackCustomBaudRate(_canConfig.Prescale, _canConfig.Prop + _canConfig.Phase1, _canConfig.Phase2, _canConfig.Sjw);
                    }
                    else
                    {
                        baudrate = (ulong)_canConfig.CanBitrate;
                    }
                    status = NiXnetLibDLL.nxSetProperty64(inSession, NiXnetLibDLL.NX_PROP_SESSION_INTF_BAUD_RATE64, 8, ref baudrate);
                    if (status != NiXnetLibDLL.NX_SUCCESS)
                        throw new Exception($"Set input session baudrate failed({NiXnetLibDLL.GetStatusString(status)})");
                    status = NiXnetLibDLL.nxSetProperty64(outSession, NiXnetLibDLL.NX_PROP_SESSION_INTF_BAUD_RATE64, 8, ref baudrate);
                    if (status != NiXnetLibDLL.NX_SUCCESS)
                        throw new Exception($"Set output session baudrate failed({NiXnetLibDLL.GetStatusString(status)})");

                    // 会话默认 AutoStart,首次读写时自动启动接口,无需显式 nxStart
                    _inputSession = inSession;
                    _outputSession = outSession;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已创建的会话
                    if (inSession != 0) { try { NiXnetLibDLL.nxClear(inSession); } catch { } }
                    if (outSession != 0) { try { NiXnetLibDLL.nxClear(outSession); } catch { } }
                    _inputSession = 0;
                    _outputSession = 0;
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

                // 关闭会话
                if (_inputSession != 0)
                {
                    try { NiXnetLibDLL.nxClear(_inputSession); } catch { }
                    _inputSession = 0;
                }
                if (_outputSession != 0)
                {
                    try { NiXnetLibDLL.nxClear(_outputSession); } catch { }
                    _outputSession = 0;
                }
                _isOpen = false;
            }
            return true;
        }

        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (_inputSession != 0) NiXnetLibDLL.nxFlush(_inputSession);
            }
        }

        public void FlushTransmitBuffer()
        {
            lock (_lock)
            {
                if (_outputSession != 0) NiXnetLibDLL.nxFlush(_outputSession);
            }
        }

        private async Task ReadTaskProc(CancellationToken token)
        {
            byte[] buffer = new byte[NiXnetLibDLL.NX_READ_BUFFER_SIZE];
            while (!token.IsCancellationRequested)
            {
                try
                {
                    uint session = _inputSession;
                    if (session == 0) break;

                    // nxReadFrame 阻塞至超时(0.1s),流内可能串联多帧,需逐帧解析
                    int status = NiXnetLibDLL.nxReadFrame(session, buffer, (uint)buffer.Length, NiXnetLibDLL.NX_READ_TIMEOUT, out uint bytesRead);
                    if (status != NiXnetLibDLL.NX_SUCCESS || bytesRead < NiXnetLibDLL.NX_FRAME_HEADER_SIZE) continue;

                    ParseFrames(buffer, bytesRead);
                }
                catch (Exception)
                {
                    // 其他异常，稍等后继续
                    try { if (!token.IsCancellationRequested) await Task.Delay(10, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }

        private void ParseFrames(byte[] buffer, uint bytesRead)
        {
            int offset = 0;
            while (offset + NiXnetLibDLL.NX_FRAME_HEADER_SIZE <= (int)bytesRead)
            {
                int frameStart = offset;
                ulong timestamp = BitConverter.ToUInt64(buffer, frameStart);
                uint identifier = BitConverter.ToUInt32(buffer, frameStart + 8);
                byte frameType = buffer[frameStart + 12];
                byte frameFlags = buffer[frameStart + 13];
                int payloadLength = buffer[frameStart + 15];
                int frameSize = NiXnetLibDLL.GetFrameSize(payloadLength);
                if (frameStart + frameSize > (int)bytesRead) break;
                offset = frameStart + frameSize;

                // 跳过发送回显与非 CAN 帧(延迟/触发等特殊帧 Type >= 0xE0)
                if ((frameFlags & NiXnetLibDLL.NX_FRAME_FLAG_TRANSMIT_ECHO) != 0) continue;
                if (frameType > NiXnetLibDLL.NX_FRAME_TYPE_CANFDBRS_DATA) continue;

                //将 EFF/RTR/ERR 标志合成到 ID 高位(SocketCAN 风格),XNET 扩展帧标记为 bit29
                long id = identifier & NiXnetLibDLL.NX_CAN_ID_MASK;
                if ((identifier & NiXnetLibDLL.NX_CAN_EXT_FLAG) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                if (frameType == NiXnetLibDLL.NX_FRAME_TYPE_CAN_REMOTE) id |= CanFrameDef.CAN_RTR_FLAG;
                if (frameType == NiXnetLibDLL.NX_FRAME_TYPE_CAN_BUS_ERROR) id |= CanFrameDef.CAN_ERR_FLAG;

                CANFrame frame1 = new CANFrame();
                frame1.ID = id;
                //钳制dlc到经典CAN有效范围，防止驱动返回异常值导致越界
                frame1.DLC = Math.Min(payloadLength, DataLenConverter.MaxCanDataLength);
                frame1.Flag = (frameFlags << 8) | frameType;
                frame1.Flags = NiXnetCanFlagParser.Parse(identifier, frameType, frameFlags);
                //XNET 时间戳单位为 100ns,统一换算为微秒
                frame1.Timestamp = timestamp / 10;
                frame1.Data = new byte[frame1.DLC];
                frame1.Dir = 1;
                Array.Copy(buffer, frameStart + NiXnetLibDLL.NX_FRAME_HEADER_SIZE, frame1.Data, 0, frame1.DLC);

                eventHandler?.Invoke(this, frame1);
            }
        }

        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _outputSession == 0) return false;

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
                    uint identifier = CanFrameHelper.GetId(canFrame.ID);
                    //XNET 扩展帧标记为 bit29(0x20000000)
                    if (CanFrameHelper.IsEFF(canFrame.ID)) identifier |= NiXnetLibDLL.NX_CAN_EXT_FLAG;
                    byte frameType = CanFrameHelper.IsRTR(canFrame.ID) ? NiXnetLibDLL.NX_FRAME_TYPE_CAN_REMOTE : NiXnetLibDLL.NX_FRAME_TYPE_CAN_DATA;

                    byte[] buffer = NiXnetLibDLL.BuildFrameBytes(identifier, frameType, canFrame.Data, canFrame.DLC);
                    int status = NiXnetLibDLL.nxWriteFrame(_outputSession, buffer, (uint)buffer.Length, 0);
                    if (status != NiXnetLibDLL.NX_SUCCESS) return false;
                    canFrame.Flag = frameType;
                    canFrame.Flags = NiXnetCanFlagParser.Parse(identifier, frameType, NiXnetLibDLL.NX_FRAME_FLAG_TRANSMIT_ECHO);
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
