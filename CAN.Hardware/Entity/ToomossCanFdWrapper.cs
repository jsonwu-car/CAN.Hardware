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
    /// 图莫斯(Toomoss) USB2XXX CANFD 封装(CANFD_* API)。
    /// 同一 CANFD 通道也可收发经典 CAN 帧(FDF=0)。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Toomoss, ToomossDeviceType.Generic, CommProtocol.CANFD)]
    public class ToomossCanFdWrapper : IHardware
    {
        private readonly ToomossCanFDConfig _canfdConfig = new ToomossCanFDConfig();

        private int _openedDevHandle = 0;
        private byte _openedChannel = 0;

        private const int RxBatchSize = 100;
        private IntPtr _rxBuffer = IntPtr.Zero;
        private int _rxFrameSize = 0;
        private IntPtr _txBuffer = IntPtr.Zero;

        public EventHandler<FrameBase>? eventHandler { get; set; }
        public BusConfigBase Config => _canfdConfig;

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
                IntPtr txBuffer = IntPtr.Zero;

                try
                {
                    if (!ToomossLibDLL.OpenDevice(_canfdConfig.DeviceIndex, out devHandle))
                        throw new Exception("Open device failed");
                    deviceOpened = true;

                    ToomossLibDLL.CANFD_INIT_CONFIG initConfig = new ToomossLibDLL.CANFD_INIT_CONFIG();
                    initConfig.__Res0 = new byte[8];

                    int ret = ToomossLibDLL.CANFD_GetCANSpeedArg(
                        devHandle, ref initConfig, _canfdConfig.ArbitrationBitrate, _canfdConfig.DataBitrate);
                    if (ret != ToomossLibDLL.CANFD_SUCCESS)
                        throw new Exception($"Get CANFD speed arg failed({ret})");

                    initConfig.Mode = (byte)_canfdConfig.Mode;
                    initConfig.ISOCRCEnable = _canfdConfig.EnableISOCRC ? (byte)1 : (byte)0;
                    initConfig.RetrySend = _canfdConfig.EnableRetrySend ? (byte)1 : (byte)0;
                    initConfig.ResEnable = _canfdConfig.EnableA120Ohm ? (byte)1 : (byte)0;

                    ret = ToomossLibDLL.CANFD_Init(devHandle, _canfdConfig.ChannelIndex, ref initConfig);
                    if (ret != ToomossLibDLL.CANFD_SUCCESS)
                        throw new Exception($"Initialize CANFD failed({ret})");

                    //CANFD 不配置过滤器时默认接收所有帧
                    if (_canfdConfig.EnableFilter)
                    {
                        if (_canfdConfig.FilterIndex > 31)
                            throw new ArgumentException($"FilterIndex must be 0-31, got {_canfdConfig.FilterIndex}.");
                        //ID_Mask 始终比较 bit31(IDE),从而只接收标准帧或扩展帧
                        uint ideAccept = _canfdConfig.ExtendedFilter ? ToomossLibDLL.CANFD_IDE_FLAG : 0;
                        uint ideMask = ToomossLibDLL.CANFD_IDE_FLAG;
                        ToomossLibDLL.CANFD_FILTER_CONFIG filter = new ToomossLibDLL.CANFD_FILTER_CONFIG
                        {
                            Enable = 1,
                            Index = _canfdConfig.FilterIndex,
                            ID_Accept = ideAccept | (_canfdConfig.FilterId & ToomossLibDLL.CANFD_ID_MASK),
                            ID_Mask = ideMask | (_canfdConfig.FilterMask & ToomossLibDLL.CANFD_ID_MASK),
                        };
                        ret = ToomossLibDLL.CANFD_SetFilter(devHandle, _canfdConfig.ChannelIndex, ref filter, 1);
                        if (ret != ToomossLibDLL.CANFD_SUCCESS)
                            throw new Exception($"Set CANFD filter failed({ret})");
                    }

                    _rxFrameSize = Marshal.SizeOf<ToomossLibDLL.CANFD_MSG>();
                    rxBuffer = Marshal.AllocHGlobal(_rxFrameSize * RxBatchSize);
                    txBuffer = Marshal.AllocHGlobal(_rxFrameSize);

                    ret = ToomossLibDLL.CANFD_StartGetMsg(devHandle, _canfdConfig.ChannelIndex);
                    if (ret != ToomossLibDLL.CANFD_SUCCESS)
                        throw new Exception($"Start CANFD receive failed({ret})");

                    _openedDevHandle = devHandle;
                    _openedChannel = _canfdConfig.ChannelIndex;
                    _rxBuffer = rxBuffer;
                    _txBuffer = txBuffer;
                    rxBuffer = IntPtr.Zero;
                    txBuffer = IntPtr.Zero;

                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    if (rxBuffer != IntPtr.Zero) Marshal.FreeHGlobal(rxBuffer);
                    if (txBuffer != IntPtr.Zero) Marshal.FreeHGlobal(txBuffer);
                    FreeBuffers();
                    if (deviceOpened)
                    {
                        try { ToomossLibDLL.CANFD_StopGetMsg(devHandle, _canfdConfig.ChannelIndex); } catch { }
                        try { ToomossLibDLL.CANFD_Stop(devHandle, _canfdConfig.ChannelIndex); } catch { }
                        try { ToomossLibDLL.CloseDevice(devHandle); } catch { }
                    }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
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
                    try { ToomossLibDLL.CANFD_StopGetMsg(_openedDevHandle, _openedChannel); } catch { }
                    try { ToomossLibDLL.CANFD_Stop(_openedDevHandle, _openedChannel); } catch { }
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
                if (_isOpen) ToomossLibDLL.CANFD_ClearMsg(_openedDevHandle, _openedChannel);
            }
        }

        public void FlushTransmitBuffer()
        {
            //USB2XXX CANFD 没有独立的发送队列清空接口
        }

        private void FreeBuffers()
        {
            if (_rxBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(_rxBuffer); _rxBuffer = IntPtr.Zero; }
            if (_txBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(_txBuffer); _txBuffer = IntPtr.Zero; }
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
                    int receiveNum = ToomossLibDLL.CANFD_GetMsg(
                        _openedDevHandle, _openedChannel, _rxBuffer, RxBatchSize);
                    if (receiveNum < 0)
                        throw new Exception($"Get CANFD message failed({receiveNum})");
                    if (receiveNum == 0) return false;

                    int count = Math.Min(receiveNum, RxBatchSize);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr pFrame = IntPtr.Add(_rxBuffer, i * _rxFrameSize);
                        var recData = Marshal.PtrToStructure<ToomossLibDLL.CANFD_MSG>(pFrame);
                        if (recData.Data == null) continue;

                        //bit7=发送帧标志:发送回显由 Send 成功后自行触发,这里丢弃
                        if ((recData.Flags & ToomossLibDLL.CANFD_FLAG_TX) != 0) continue;

                        CANFrame frame = new CANFrame();
                        //保留 bit31(IDE)/bit30(RTR)/bit28..0(ID),清除 bit29
                        frame.ID = recData.ID & 0xDFFFFFFFu;
                        bool isFd = (recData.Flags & ToomossLibDLL.CANFD_FLAG_FDF) != 0;
                        frame.DLC = Math.Min((int)recData.DLC,
                            isFd ? DataLenConverter.MaxCanFdDataLength : DataLenConverter.MaxCanDataLength);
                        frame.Flag = recData.Flags;
                        frame.Flags = ToomossCanFdFlagParser.Parse(recData.ID, recData.Flags, false);
                        frame.FDF = isFd;
                        frame.BRS = (recData.Flags & ToomossLibDLL.CANFD_FLAG_BRS) != 0;

                        //时间戳单位 10us,换算为微秒
                        ulong timestamp = ((ulong)recData.TimeStampHigh << 32) | recData.TimeStamp;
                        frame.Timestamp = timestamp * 10UL;

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
                if (!_isOpen || _txBuffer == IntPtr.Zero) return false;
                if (frame is not CANFrame canFrame) return false;

                try
                {
                    return canFrame.FDF ? SendFd(canFrame) : SendCan(canFrame);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }

        private bool SendCan(CANFrame canFrame)
        {
            if (canFrame.BRS)
                throw new ArgumentException("BRS is only valid for CANFD frame(FDF must be set).");
            if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
                throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");

            bool isRTR = CanFrameHelper.IsRTR(canFrame.ID);
            if (!isRTR && (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC))
                throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

            canFrame.Dir = 0;
            uint validId = CanFrameHelper.GetId(canFrame.ID);
            bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);

            uint rawId = (isExtended ? ToomossLibDLL.CANFD_IDE_FLAG : 0)
                       | (isRTR ? ToomossLibDLL.CANFD_RTR_FLAG : 0)
                       | (validId & ToomossLibDLL.CANFD_ID_MASK);

            ToomossLibDLL.CANFD_MSG sendMsg = new ToomossLibDLL.CANFD_MSG
            {
                ID = rawId,
                DLC = (byte)canFrame.DLC,
                Flags = 0,
                TimeStamp = 0,
                TimeStampHigh = 0,
                Data = new byte[DataLenConverter.MaxCanFdDataLength],
            };
            if (!isRTR && canFrame.DLC > 0)
                Array.Copy(canFrame.Data, 0, sendMsg.Data, 0, canFrame.DLC);

            Marshal.StructureToPtr(sendMsg, _txBuffer, true);
            int sentNum = ToomossLibDLL.CANFD_SendMsg(_openedDevHandle, _openedChannel, _txBuffer, 1);
            if (sentNum != 1) return false;

            canFrame.Flags = ToomossCanFdFlagParser.Parse(sendMsg.ID, sendMsg.Flags, true);
            eventHandler?.Invoke(this, canFrame);
            return true;
        }

        private bool SendFd(CANFrame canFrame)
        {
            if (canFrame.BRS && !canFrame.FDF)
                throw new ArgumentException("BRS is only valid for CANFD frame(FDF must be set).");
            if (!DataLenConverter.IsValidCanFdLength(canFrame.DLC))
                throw new ArgumentException($"Invalid CANFD DLC({canFrame.DLC}), valid lengths: 0-8,12,16,20,24,32,48,64.");
            if (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC)
                throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

            canFrame.Dir = 0;
            uint validId = CanFrameHelper.GetId(canFrame.ID);
            bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);
            bool isRTR = CanFrameHelper.IsRTR(canFrame.ID);

            uint rawId = (isExtended ? ToomossLibDLL.CANFD_IDE_FLAG : 0)
                       | (isRTR ? ToomossLibDLL.CANFD_RTR_FLAG : 0)
                       | (validId & ToomossLibDLL.CANFD_ID_MASK);
            byte flags = (byte)((canFrame.FDF ? ToomossLibDLL.CANFD_FLAG_FDF : 0)
                              | (canFrame.BRS ? ToomossLibDLL.CANFD_FLAG_BRS : 0));

            ToomossLibDLL.CANFD_MSG sendMsg = new ToomossLibDLL.CANFD_MSG
            {
                ID = rawId,
                DLC = (byte)canFrame.DLC,
                Flags = flags,
                TimeStamp = 0,
                TimeStampHigh = 0,
                Data = new byte[DataLenConverter.MaxCanFdDataLength],
            };
            Array.Copy(canFrame.Data, 0, sendMsg.Data, 0, canFrame.DLC);

            Marshal.StructureToPtr(sendMsg, _txBuffer, true);
            int sentNum = ToomossLibDLL.CANFD_SendMsg(_openedDevHandle, _openedChannel, _txBuffer, 1);
            if (sentNum != 1) return false;

            canFrame.Flags = ToomossCanFdFlagParser.Parse(sendMsg.ID, sendMsg.Flags, true);
            eventHandler?.Invoke(this, canFrame);
            return true;
        }
    }
}
