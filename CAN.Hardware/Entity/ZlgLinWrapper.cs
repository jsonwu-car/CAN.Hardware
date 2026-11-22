using System.Runtime.InteropServices;
using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    /// <summary>
    /// 周立功(ZLG) LIN(zlgcan.dll)封装。
    /// 三种节点模式的语义:
    /// - Master:Send 先配置发布表再发送帧头(报头),RequestMessage 只发帧头,应答由后台任务经事件上抛;
    /// - Slave:Send 只更新发布表,收到匹配帧头时由设备自动应答;
    /// - Monitor:ZCAN_LIN_INIT_CONFIG 只有 slave/master 两种角色,不支持纯监听,Open 时抛异常。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Zlg, ZlgDeviceType.USBCANFD200U, CommProtocol.LIN)]
    public class ZlgLinWrapper : ILinHardware
    {
        private const int LinMaxDlc = 8;
        private const long LinMaxId = 0x3F;
        private const int ReceiveBatchSize = 16;
        private const uint MinBitrate = 1000;
        private const uint MaxBitrate = 20000;

        // ZCAN_LIN_MSG.data 内 ZCANLINData(dataType=0) 的字段偏移(Pack=1)
        private const int LinPidOffset = 0;
        private const int LinTimestampOffset = 1;
        private const int LinDataLenOffset = 9;
        private const int LinDirOffset = 10;
        private const int LinChkSumOffset = 11;
        private const int LinPayloadOffset = 25;

        // ZCAN_LIN_MSG.data 内 ZCANLINErrData(dataType=1) 的字段偏移(Pack=1)
        private const int ErrTimestampOffset = 0;
        private const int ErrPidOffset = 8;
        private const int ErrDataLenOffset = 9;
        private const int ErrPayloadOffset = 10;
        private const int ErrDataOffset = 18;
        private const int ErrDirOffset = 20;
        private const int ErrChkSumOffset = 21;

        private readonly ZlgLinConfig _linConfig = new ZlgLinConfig();
        private uint _status = 0;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; } = null;

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _linConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();

        // Task控制相关
        private bool _isOpen = false;
        private bool _closing = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        //ZLG 句柄分两层: 设备句柄(共享,引用计数) + LIN 通道句柄
        private IntPtr _devHandle = IntPtr.Zero;
        private IntPtr _channelHandle = IntPtr.Zero;
        private IntPtr _receiveBuffer = IntPtr.Zero;
        private readonly Queue<LINFrame> _pendingFrames = new Queue<LINFrame>();

        public object GetConfig() { return _linConfig; }

        public bool Close()
        {
            Task? taskToWait;
            CancellationTokenSource? ctsToCancel;
            IntPtr channelToReset;
            IntPtr bufferToFree;

            lock (_lock)
            {
                if (!_isOpen) return true;
                _isOpen = false;
                _closing = true;
                channelToReset = _channelHandle;
                ctsToCancel = _cts;
                taskToWait = _readTask;
                bufferToFree = _receiveBuffer;
                _cts = null;
                _readTask = null;
                _receiveBuffer = IntPtr.Zero;
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
                if (channelToReset != IntPtr.Zero)
                {
                    try { _status = ZLGCanLibDLL.ZCAN_ResetLIN(channelToReset); } catch { }
                }
                try { ZLGCanLibDLL.CloseDevice(); } catch { }
                if (bufferToFree != IntPtr.Zero)
                {
                    try { Marshal.FreeHGlobal(bufferToFree); } catch { }
                }
                _channelHandle = IntPtr.Zero;
                _devHandle = IntPtr.Zero;
                _pendingFrames.Clear();
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
                if (!_isOpen || _channelHandle == IntPtr.Zero) return;
                try { _status = ZLGCanLibDLL.ZCAN_ClearBuffer(_channelHandle); } catch { }
                _pendingFrames.Clear();
            }
        }

        // ZLG LIN 无清空发送缓冲区的 API(ZCAN_ClearBuffer 清的是接收缓冲),保持空实现
        public void FlushTransmitBuffer() { }

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;
                if (_closing) return false;
                if (!Enum.IsDefined(typeof(LinNodeMode), _linConfig.NodeMode) || _linConfig.NodeMode == LinNodeMode.Monitor)
                    throw new Exception($"Unsupported node type({_linConfig.NodeMode})");
                if (_linConfig.LinBitrate < MinBitrate || _linConfig.LinBitrate > MaxBitrate)
                    throw new Exception($"Unsupported bitrate({_linConfig.LinBitrate})");
                if (_linConfig.MaxLength < LinMaxDlc || _linConfig.MaxLength > 64)
                    throw new Exception($"Unsupported max length({_linConfig.MaxLength})");

                IntPtr devHandle = IntPtr.Zero;
                IntPtr channelHandle = IntPtr.Zero;
                IntPtr receiveBuffer = IntPtr.Zero;
                try
                {
                    // 打开设备(同一物理设备共享句柄,多通道复用)
                    devHandle = ZLGCanLibDLL.OpenDevice((uint)_linConfig.DeviceModel, _linConfig.DeviceIndex, 0);
                    if (devHandle == IntPtr.Zero) throw new Exception("Open device failed");

                    // 初始化 LIN 通道: linMode 0=slave/1=master,校验方式与最大长度随初始化下发
                    ZLGCanLibDLL.ZCAN_LIN_INIT_CONFIG initConfig = new ZLGCanLibDLL.ZCAN_LIN_INIT_CONFIG();
                    initConfig.linMode = (byte)(_linConfig.NodeMode == LinNodeMode.Master ? 1 : 0);
                    initConfig.chkSumMode = (byte)_linConfig.ChkSumMode;
                    initConfig.maxLength = _linConfig.MaxLength;
                    initConfig.reserved = 0;
                    initConfig.linBaud = _linConfig.LinBitrate;
                    IntPtr pConfig = Marshal.AllocHGlobal(Marshal.SizeOf(initConfig));
                    try
                    {
                        Marshal.StructureToPtr(initConfig, pConfig, true);
                        channelHandle = ZLGCanLibDLL.ZCAN_InitLIN(devHandle, _linConfig.DeviceChannel, pConfig);
                    }
                    finally { Marshal.FreeHGlobal(pConfig); }
                    if (channelHandle == IntPtr.Zero) throw new Exception("Initialize lin failed");

                    // 内部上拉终端电阻(InitLIN 之后设置)
                    _status = ZLGCanLibDLL.ZCAN_SetValue(devHandle, $"{_linConfig.DeviceChannel}/lin_initenal_resistance", _linConfig.EnableInternalResistor ? "1" : "0");
                    if (_status != 1) throw new Exception($"Set lin internal resistor failed({_status})");

                    // 启动 LIN 通道
                    _status = ZLGCanLibDLL.ZCAN_StartLIN(channelHandle);
                    if (_status != 1) throw new Exception($"Start lin failed({_status})");

                    // 接收缓冲区在 Open 时一次性分配,避免每帧 AllocHGlobal/FreeHGlobal
                    receiveBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ZLGCanLibDLL.ZCAN_LIN_MSG)) * ReceiveBatchSize);

                    // 所有操作成功后，才提交状态
                    _devHandle = devHandle;
                    _channelHandle = channelHandle;
                    _receiveBuffer = receiveBuffer;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (receiveBuffer != IntPtr.Zero) { try { Marshal.FreeHGlobal(receiveBuffer); } catch { } }
                    if (channelHandle != IntPtr.Zero) { try { ZLGCanLibDLL.ZCAN_ResetLIN(channelHandle); } catch { } }
                    if (devHandle != IntPtr.Zero) { try { ZLGCanLibDLL.CloseDevice(); } catch { } }
                    if (_cts != null) { try { _cts.Dispose(); } catch { } _cts = null; }
                    _readTask = null;
                    _isOpen = false;
                    _channelHandle = IntPtr.Zero;
                    _devHandle = IntPtr.Zero;
                    _receiveBuffer = IntPtr.Zero;
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
                if (!_isOpen || _channelHandle == IntPtr.Zero)
                    throw new Exception($"{nameof(RequestMessage)}:LIN channel is not open");
                if (_linConfig.NodeMode != LinNodeMode.Master)
                    throw new Exception($"{nameof(RequestMessage)}:Not in master mode({_linConfig.NodeMode})");
                try
                {
                    // 仅发送帧头,请求从节点数据(数据长度由从节点响应决定)
                    ZLGCanLibDLL.ZCAN_LIN_MSG request = CreateLinMsg((byte)linId);
                    uint result = TransmitLinLocked(ref request);
                    if (result != 1)
                        throw new Exception($"Request message failed(status:{result})");
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(RequestMessage)}:{ex.Message}");
                }
            }
        }

        // ZLG 没有独立的运行时校验和切换 API:初始化时下发 chkSumMode,
        // 之后的发送由发布表逐条携带校验方式。未打开时更新配置,
        // 已打开时更新配置并对后续 Send 生效。
        public bool SetChecksumType(bool enhanced)
        {
            lock (_lock)
            {
                _linConfig.ChkSumMode = enhanced ? ZlgLinChkSumMode.Enhanced : ZlgLinChkSumMode.Classic;
                return true;
            }
        }

        public bool SetIllegalMessage(long linId, bool checksumError, bool parityError, uint delayBitTimes)
        {
            throw new NotSupportedException($"{nameof(SetIllegalMessage)}:ZLG LIN API 当前未暴露等价的错误注入能力");
        }

        public bool ClearMessage(long linId)
        {
            if (linId < 0 || linId > LinMaxId)
                throw new Exception($"{nameof(ClearMessage)}:Invalid frame id({linId})");
            lock (_lock)
            {
                if (!_isOpen || _channelHandle == IntPtr.Zero)
                    throw new Exception($"{nameof(ClearMessage)}:LIN channel is not open");
                if (_linConfig.NodeMode != LinNodeMode.Slave)
                    throw new Exception($"{nameof(ClearMessage)}:Not in slave mode({_linConfig.NodeMode})");
                try
                {
                    // 官方 demo(SetLINSubscribe_Test)的"取消响应":ZLG 发布表没有删除表项的 API,
                    // 对该 ID 改为订阅配置(仅接收、不应答)以覆盖原发布表项,固件即停止自动响应;
                    // dataLen=0xFF 表示设备自动识别报文长度,chkSumMode=3(自动)仅订阅配置有效
                    ZLGCanLibDLL.ZCAN_LIN_SUBSCIBE_CFG subscribe = new ZLGCanLibDLL.ZCAN_LIN_SUBSCIBE_CFG();
                    subscribe.ID = (byte)linId;
                    subscribe.dataLen = 0xFF;
                    subscribe.chkSumMode = 3;
                    subscribe.reserved = new byte[5];
                    IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(subscribe));
                    try
                    {
                        Marshal.StructureToPtr(subscribe, ptr, true);
                        _status = ZLGCanLibDLL.ZCAN_SetLINSubscribe(_channelHandle, ptr, 1);
                    }
                    finally { Marshal.FreeHGlobal(ptr); }
                    if (_status != 1)
                        throw new Exception($"Clear message failed(status:{_status})");
                    return true;
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
                if (!_isOpen || _channelHandle == IntPtr.Zero)
                    throw new Exception($"{nameof(WakeUp)}:LIN channel is not open");
                if (_linConfig.NodeMode != LinNodeMode.Master)
                    throw new Exception($"{nameof(WakeUp)}:Not in master mode({_linConfig.NodeMode})");
                try
                {
                    _status = ZLGCanLibDLL.ZCAN_WakeUpLIN(_channelHandle);
                    if (_status != 1)
                        throw new Exception($"Wake up failed(status:{_status})");
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
                if (!_isOpen || _channelHandle == IntPtr.Zero)
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
            // 官方 demo(LIN_Send_Test):ZCAN_TransmitLIN 只发送帧头(PID),帧数据必须先通过
            // ZCAN_SetLINPublish 配置到发布表;主机发送带数据的帧 = 配置发布表 + 发送帧头
            if (!SetPublishLocked(linFrame)) return false;
            if (_linConfig.NodeMode == LinNodeMode.Master)
            {
                ZLGCanLibDLL.ZCAN_LIN_MSG msg = CreateLinMsg((byte)linFrame.ID);
                if (TransmitLinLocked(ref msg) != 1) return false;
            }
            return true;
        }

        private bool SetPublishLocked(LINFrame linFrame)
        {
            ZLGCanLibDLL.ZCAN_LIN_PUBLISH_CFG publish = new ZLGCanLibDLL.ZCAN_LIN_PUBLISH_CFG();
            publish.ID = (byte)linFrame.ID;
            publish.dataLen = (byte)linFrame.DLC;
            publish.data = new byte[LinMaxDlc];
            Array.Copy(linFrame.Data, publish.data, linFrame.DLC);
            // ZCAN_LIN_PUBLISH_CFG 的 chkSumMode:0-默认(启动时配置) 1-经典 2-增强,不支持自动;
            // Auto 时填 0,由初始化时配置的校验方式决定
            publish.chkSumMode = _linConfig.ChkSumMode == ZlgLinChkSumMode.Auto
                ? (byte)0
                : (byte)_linConfig.ChkSumMode;
            publish.reserved = new byte[5];
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(publish));
            try
            {
                Marshal.StructureToPtr(publish, ptr, true);
                _status = ZLGCanLibDLL.ZCAN_SetLINPublish(_channelHandle, ptr, 1);
            }
            finally { Marshal.FreeHGlobal(ptr); }
            return _status == 1;
        }

        private bool TryReadFrameLocked(out LINFrame? frame)
        {
            frame = null;
            if (!_isOpen || _channelHandle == IntPtr.Zero || _receiveBuffer == IntPtr.Zero) return false;

            if (_pendingFrames.Count == 0)
            {
                if (ZLGCanLibDLL.ZCAN_GetLINReceiveNum(_channelHandle) == 0) return false;
                uint count = ZLGCanLibDLL.ZCAN_ReceiveLIN(_channelHandle, _receiveBuffer, ReceiveBatchSize, 0);
                int structSize = Marshal.SizeOf(typeof(ZLGCanLibDLL.ZCAN_LIN_MSG));
                for (int i = 0; i < (int)count && i < ReceiveBatchSize; i++)
                {
                    ZLGCanLibDLL.ZCAN_LIN_MSG msg = Marshal.PtrToStructure<ZLGCanLibDLL.ZCAN_LIN_MSG>(
                        IntPtr.Add(_receiveBuffer, i * structSize));
                    LINFrame? parsed = BuildFrame(msg);
                    if (parsed != null) _pendingFrames.Enqueue(parsed);
                }
            }
            if (_pendingFrames.Count == 0) return false;
            frame = _pendingFrames.Dequeue();
            return true;
        }

        // 按官方 demo:发送仅需填 PID(裸 ID,校验位由固件计算),其余字段保持 0,即仅发送帧头
        private static ZLGCanLibDLL.ZCAN_LIN_MSG CreateLinMsg(byte id)
        {
            ZLGCanLibDLL.ZCAN_LIN_MSG msg = new ZLGCanLibDLL.ZCAN_LIN_MSG();
            msg.chnl = 0;
            msg.dataType = 0;
            msg.data = new byte[46];
            msg.data[LinPidOffset] = id;
            return msg;
        }

        private uint TransmitLinLocked(ref ZLGCanLibDLL.ZCAN_LIN_MSG msg)
        {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(msg));
            try
            {
                Marshal.StructureToPtr(msg, ptr, true);
                return ZLGCanLibDLL.ZCAN_TransmitLIN(_channelHandle, ptr, 1);
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }

        private static LINFrame? BuildFrame(ZLGCanLibDLL.ZCAN_LIN_MSG msg)
        {
            if (msg.data == null || msg.data.Length < 46) return null;
            byte[] raw = msg.data;

            byte pid, dataLen, dir, chkSum, errorStatus = 0;
            ulong timestamp;
            int payloadOffset;

            if (msg.dataType == 1)
            {
                timestamp = BitConverter.ToUInt64(raw, ErrTimestampOffset);
                pid = raw[ErrPidOffset];
                dataLen = raw[ErrDataLenOffset];
                dir = raw[ErrDirOffset];
                chkSum = raw[ErrChkSumOffset];
                errorStatus = raw[ErrDataOffset];
                payloadOffset = ErrPayloadOffset;
            }
            else if (msg.dataType == 0)
            {
                timestamp = BitConverter.ToUInt64(raw, LinTimestampOffset);
                pid = raw[LinPidOffset];
                dataLen = raw[LinDataLenOffset];
                dir = raw[LinDirOffset];
                chkSum = raw[LinChkSumOffset];
                payloadOffset = LinPayloadOffset;
            }
            else
            {
                return null;
            }

            if (dataLen > LinMaxDlc) dataLen = LinMaxDlc;
            LINFrame frame = new LINFrame();
            frame.ID = pid & 0x3F;
            frame.DLC = dataLen;
            frame.Data = new byte[dataLen];
            Array.Copy(raw, payloadOffset, frame.Data, 0, dataLen);
            frame.Flag = msg.dataType;
            frame.Flags = ZlgLinFlagParser.Parse(msg.dataType, dir, pid, chkSum, errorStatus);
            frame.Timestamp = timestamp;
            frame.Dir = dir == 1 ? 0 : 1;
            return frame;
        }
    }
}
