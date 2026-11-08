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
    /// 图莫斯(Toomoss) USB2XXX 经典 CAN 封装(CAN_* API)。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Toomoss, ToomossDeviceType.Generic, CommProtocol.CAN)]
    public class ToomossCanWrapper : IHardware
    {
        private readonly ToomossCANConfig _canConfig = new ToomossCANConfig();

        private int _openedDevHandle = 0;
        private byte _openedChannel = 0;

        private const int RxBatchSize = 100;
        private IntPtr _rxBuffer = IntPtr.Zero;
        private int _rxFrameSize = 0;

        public EventHandler<FrameBase>? eventHandler { get; set; }
        public BusConfigBase Config => _canConfig;

        private readonly object _lock = new object();
        private bool _isOpen = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                int devHandle = 0;
                bool deviceOpened = false;
                IntPtr rxBuffer = IntPtr.Zero;

                try
                {
                    if (!ToomossLibDLL.OpenDevice(_canConfig.DeviceIndex, out devHandle))
                        throw new Exception("Open device failed");
                    deviceOpened = true;

                    ToomossLibDLL.CAN_INIT_CONFIG initConfig = new ToomossLibDLL.CAN_INIT_CONFIG();
                    int ret = ToomossLibDLL.CAN_GetCANSpeedArg(devHandle, ref initConfig, _canConfig.CanBitrate);
                    if (ret != ToomossLibDLL.CAN_SUCCESS)
                        throw new Exception($"Get CAN speed arg failed({ret})");

                    initConfig.CAN_Mode = (byte)((int)_canConfig.Mode | (_canConfig.EnableA120Ohm ? ToomossLibDLL.CAN_MODE_RESISTOR : 0));
                    initConfig.CAN_ABOM = _canConfig.EnableAutoOffline ? (byte)1 : (byte)0;
                    initConfig.CAN_NART = _canConfig.DisableRetransmission ? (byte)1 : (byte)0;
                    initConfig.CAN_RFLM = _canConfig.FifoLock ? (byte)1 : (byte)0;
                    initConfig.CAN_TXFP = _canConfig.TxPriorityByRequest ? (byte)1 : (byte)0;

                    ret = ToomossLibDLL.CAN_Init(devHandle, _canConfig.ChannelIndex, ref initConfig);
                    if (ret != ToomossLibDLL.CAN_SUCCESS)
                        throw new Exception($"Initialize CAN failed({ret})");

                    //经典 CAN 必须配置过滤器,不启用过滤时配置为接收所有帧
                    if (_canConfig.EnableFilter && _canConfig.FilterIndex > 13)
                        throw new ArgumentException($"FilterIndex must be 0-13, got {_canConfig.FilterIndex}.");
                    ToomossLibDLL.CAN_FILTER_CONFIG filter = BuildFilter();
                    ret = ToomossLibDLL.CAN_Filter_Init(devHandle, _canConfig.ChannelIndex, ref filter);
                    if (ret != ToomossLibDLL.CAN_SUCCESS)
                        throw new Exception($"Set CAN filter failed({ret})");

                    _rxFrameSize = Marshal.SizeOf<ToomossLibDLL.CAN_MSG>();
                    rxBuffer = Marshal.AllocHGlobal(_rxFrameSize * RxBatchSize);

                    ret = ToomossLibDLL.CAN_StartGetMsg(devHandle, _canConfig.ChannelIndex);
                    if (ret != ToomossLibDLL.CAN_SUCCESS)
                        throw new Exception($"Start CAN receive failed({ret})");

                    _openedDevHandle = devHandle;
                    _openedChannel = _canConfig.ChannelIndex;
                    _rxBuffer = rxBuffer;
                    rxBuffer = IntPtr.Zero;

                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    if (rxBuffer != IntPtr.Zero) Marshal.FreeHGlobal(rxBuffer);
                    FreeBuffers();
                    if (deviceOpened)
                    {
                        try { ToomossLibDLL.CAN_StopGetMsg(devHandle, _canConfig.ChannelIndex); } catch { }
                        try { ToomossLibDLL.CAN_Stop(devHandle, _canConfig.ChannelIndex); } catch { }
                        try { ToomossLibDLL.CloseDevice(devHandle); } catch { }
                    }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _isOpen = false;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        private ToomossLibDLL.CAN_FILTER_CONFIG BuildFilter()
        {
            ToomossLibDLL.CAN_FILTER_CONFIG filter = new ToomossLibDLL.CAN_FILTER_CONFIG();
            filter.Enable = 1;
            filter.FilterIndex = _canConfig.FilterIndex;
            filter.FilterMode = (byte)_canConfig.FilterMode;
            filter.ExtFrame = (byte)(_canConfig.ExtendedFilter ? 1 : 0);
            filter.ID_Std_Ext = _canConfig.FilterId & CanFrameDef.CAN_ID_FLAG;
            //IDE 位必须与 ExtFrame 保持一致
            filter.ID_IDE = _canConfig.ExtendedFilter ? 1u : 0u;
            filter.ID_RTR = _canConfig.FilterRtr;
            filter.MASK_Std_Ext = _canConfig.FilterMask;
            filter.MASK_IDE = _canConfig.FilterMaskIde;
            filter.MASK_RTR = _canConfig.FilterMaskRtr;

            if (!_canConfig.EnableFilter)
            {
                //接收所有帧:过滤器索引/模式/帧类型归零,验收码/屏蔽码全 0
                filter.FilterIndex = 0;
                filter.FilterMode = 0;
                filter.ExtFrame = 0;
                filter.ID_Std_Ext = 0;
                filter.ID_IDE = 0;
                filter.ID_RTR = 0;
                filter.MASK_Std_Ext = 0;
                filter.MASK_IDE = 0;
                filter.MASK_RTR = 0;
            }
            return filter;
        }

        public bool Close()
        {
            lock (_lock)
            {
                if (!_isOpen) return true;
                if (_cts != null) _cts.Cancel();
            }

            if (_readTask != null)
            {
                try { _readTask.Wait(1000); }
                catch (AggregateException) { }
            }

            lock (_lock)
            {
                if (_cts != null) { _cts.Dispose(); _cts = null; }
                _readTask = null;

                if (_isOpen)
                {
                    try { ToomossLibDLL.CAN_StopGetMsg(_openedDevHandle, _openedChannel); } catch { }
                    try { ToomossLibDLL.CAN_Stop(_openedDevHandle, _openedChannel); } catch { }
                    try { ToomossLibDLL.CloseDevice(_openedDevHandle); } catch { }
                    FreeBuffers();
                    _isOpen = false;
                }
            }
            return true;
        }

        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (_isOpen) ToomossLibDLL.CAN_ClearMsg(_openedDevHandle, _openedChannel);
            }
        }

        public void FlushTransmitBuffer()
        {
            //USB2XXX 经典 CAN 没有独立的发送队列清空接口
        }

        private void FreeBuffers()
        {
            if (_rxBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(_rxBuffer); _rxBuffer = IntPtr.Zero; }
            _rxFrameSize = 0;
        }

        private async Task ReadTaskProc(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    bool res;
                    do
                    {
                        if (token.IsCancellationRequested) break;
                        res = Read();
                    }
                    while (res && !token.IsCancellationRequested);

                    if (!token.IsCancellationRequested) await Task.Delay(5, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    try { if (!token.IsCancellationRequested) await Task.Delay(10, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }

        private bool Read()
        {
            lock (_lock)
            {
                if (!_isOpen || _rxBuffer == IntPtr.Zero) return false;

                try
                {
                    int receiveNum = ToomossLibDLL.CAN_GetMsgWithSize(
                        _openedDevHandle, _openedChannel, _rxBuffer, RxBatchSize);
                    if (receiveNum < 0)
                        throw new Exception($"Get CAN message failed({receiveNum})");
                    if (receiveNum == 0) return false;

                    int count = Math.Min(receiveNum, RxBatchSize);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr pFrame = IntPtr.Add(_rxBuffer, i * _rxFrameSize);
                        var recData = Marshal.PtrToStructure<ToomossLibDLL.CAN_MSG>(pFrame);
                        if (recData.Data == null) continue;

                        //bit7=发送帧标志:发送回显由 Send 成功后自行触发,这里丢弃
                        if ((recData.RemoteFlag & ToomossLibDLL.CAN_MSG_REMOTE_TX) != 0) continue;

                        CANFrame frame = new CANFrame();
                        long id = recData.ID & CanFrameDef.CAN_ID_FLAG;
                        if ((recData.ExternFlag & ToomossCanFlagParser.ExternFlagExt) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                        if ((recData.RemoteFlag & ToomossLibDLL.CAN_MSG_REMOTE_RTR) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
                        if ((recData.ExternFlag & ToomossCanFlagParser.ExternFlagErr) != 0) id |= CanFrameDef.CAN_ERR_FLAG;
                        frame.ID = id;

                        frame.DLC = Math.Min((int)recData.DataLen, DataLenConverter.MaxCanDataLength);
                        frame.Flag = recData.RemoteFlag | (recData.ExternFlag << 8);
                        frame.Flags = ToomossCanFlagParser.Parse(recData.ID, recData.RemoteFlag, recData.ExternFlag, false);

                        //时间戳单位 100us,换算为微秒
                        ulong timestamp = ((ulong)recData.TimeStampHigh << 32) | recData.TimeStamp;
                        frame.Timestamp = timestamp * 100UL;

                        frame.Data = new byte[frame.DLC];
                        if (frame.DLC > 0) Array.Copy(recData.Data, 0, frame.Data, 0, frame.DLC);
                        frame.Dir = 1;

                        eventHandler?.Invoke(this, frame);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Read)}:{ex.Message}");
                }
            }
        }

        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen) return false;
                if (frame is not CANFrame canFrame) return false;

                if (canFrame.FDF || canFrame.BRS)
                    throw new ArgumentException("Classic CAN channel does not support CANFD frame(FDF/BRS).");
                if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
                    throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");

                bool isRTR = CanFrameHelper.IsRTR(canFrame.ID);
                if (!isRTR && (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC))
                    throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

                try
                {
                    canFrame.Dir = 0;
                    uint validId = CanFrameHelper.GetId(canFrame.ID);
                    bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);

                    ToomossLibDLL.CAN_MSG sendMsg = new ToomossLibDLL.CAN_MSG
                    {
                        ID = validId,
                        TimeStamp = 0,
                        RemoteFlag = (byte)(isRTR ? ToomossLibDLL.CAN_MSG_REMOTE_RTR : 0),
                        ExternFlag = (byte)(isExtended ? 1 : 0),
                        DataLen = (byte)canFrame.DLC,
                        Data = new byte[DataLenConverter.MaxCanDataLength],
                        TimeStampHigh = 0,
                    };
                    if (!isRTR && canFrame.DLC > 0)
                        Array.Copy(canFrame.Data, 0, sendMsg.Data, 0, canFrame.DLC);

                    int sentNum = ToomossLibDLL.CAN_SendMsg(
                        _openedDevHandle, _openedChannel, new[] { sendMsg }, 1);
                    if (sentNum != 1) return false;

                    canFrame.Flags = ToomossCanFlagParser.Parse(sendMsg.ID, sendMsg.RemoteFlag, sendMsg.ExternFlag, true);
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
