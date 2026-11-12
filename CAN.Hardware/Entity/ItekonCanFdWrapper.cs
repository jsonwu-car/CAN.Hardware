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
    /// 爱泰科技(Itekon) CANFD 封装(iTekCANFD.dll / iTek_ API)。
    /// 适用于 USBCANFD / NetCANFD / WifiCANFD / PCIeCANFD 等 CANFD 系列。
    /// 通道初始化后同时支持 CANFD 与经典 CAN,由 <see cref="ItekonCanFDConfig.Protocol"/> 决定通道类型。
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Itekon, ItekonDeviceType.USBCANFD, CommProtocol.CANFD)]
    [HardwareDefAttributes(HardwareManufacturer.Itekon, ItekonDeviceType.USBCANFD, CommProtocol.CAN)]
    public class ItekonCanFdWrapper : IHardware
    {
        private readonly ItekonCanFDConfig _canfdConfig = new ItekonCanFDConfig();

        //Open 时的设备参数/模式快照,会话期间固定使用
        private ushort _openedDeviceType = 0;
        private ushort _openedDeviceIndex = 0;
        private byte _openedChannel = 0;
        private IntPtr _openedChannelHandle = IntPtr.Zero;
        //IO 句柄：使用 iTek_InitCAN 返回的通道句柄。
        //注：手册 4.4～4.7 写 NETCANFD 用设备句柄，但官方 C#/Qt 示例统一使用该通道句柄；
        //工程按示例实现，CloseDevice 仍使用 iTek_OpenDevice 返回的设备句柄（与官方 C# 示例一致）。
        private IntPtr _openedIoHandle = IntPtr.Zero;
        private ItekonCanFdProtocol _openedProtocol = ItekonCanFdProtocol.CANFD;

        //接收/发送缓冲区在 Open 时一次性分配,Close 时释放
        private const int RxBatchSize = 100;    //单次 P/Invoke 最多读取的帧数
        private IntPtr _rxBuffer = IntPtr.Zero;
        private int _rxFrameSize = 0;
        private IntPtr _txBuffer = IntPtr.Zero;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _canfdConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();

        // Task控制相关
        private bool _isOpen = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        public bool Close()
        {
            lock (_lock)
            {
                if (!_isOpen) return true;
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

                if (_isOpen)
                {
                    try { ItekonCanFdLibDLL.iTek_ResetCAN(_openedChannelHandle); } catch { }
                    try { ItekonCanFdLibDLL.CloseDevice(_openedDeviceType, _openedDeviceIndex); } catch { }
                    _openedChannelHandle = IntPtr.Zero;
                    _openedIoHandle = IntPtr.Zero;
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
                if (_isOpen) ItekonCanFdLibDLL.iTek_ClearBuffer(_openedIoHandle);
            }
        }

        public void FlushTransmitBuffer()
        {
            lock (_lock)
            {
                if (_isOpen) ItekonCanFdLibDLL.iTek_ClearBuffer(_openedIoHandle);
            }
        }

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                ushort deviceType = (ushort)_canfdConfig.DeviceModel;
                ushort deviceIndex = _canfdConfig.DeviceIndex;
                byte channel = _canfdConfig.DeviceChannel;
                ItekonCanFdProtocol protocol = _canfdConfig.Protocol;

                IntPtr devHandle = IntPtr.Zero;
                IntPtr chHandle = IntPtr.Zero;
                IntPtr rxBuffer = IntPtr.Zero;
                IntPtr txBuffer = IntPtr.Zero;
                IntPtr pInitConfig = IntPtr.Zero;

                try
                {
                    // 打开设备(同一物理设备的多个通道共享一个设备句柄)
                    devHandle = ItekonCanFdLibDLL.OpenDevice(deviceType, deviceIndex);
                    if (devHandle == IntPtr.Zero) throw new Exception("Open device failed");

                    // 网络型设备先下发 IP/端口配置(必须在 InitCAN 之前)
                    if (_canfdConfig.EnableNetConfig && ItekonCanFdLibDLL.IsNetDevice(deviceType))
                    {
                        uint setResult = ItekonCanFdLibDLL.SetIpInfo(
                            devHandle, _canfdConfig.NetMode,
                            _canfdConfig.LocalIp, _canfdConfig.LocalPort,
                            _canfdConfig.DestIp, _canfdConfig.DestPort);
                        if (setResult != 1)
                            throw new Exception($"Set network info failed({setResult})");
                    }

                    // 组装通道初始化参数
                    ItekonCanFdLibDLL.ItekCanFdChannelInitConfig initConfig =
                        ItekonCanFdLibDLL.CreateInitConfig();
                    initConfig.can_type = (byte)protocol;
                    initConfig.CANFDStandard = (byte)_canfdConfig.CANFDStandard;
                    initConfig.CANFDSpeedup = 0;
                    initConfig.abit_timing = _canfdConfig.EnableCustomBitrate
                        ? _canfdConfig.CustomArbitrationTiming
                        : ItekonCanFdLibDLL.BuildArbitrationTiming(_canfdConfig.ArbitrationBitrate);
                    initConfig.dbit_timing = _canfdConfig.EnableCustomBitrate
                        ? _canfdConfig.CustomDataTiming
                        : ItekonCanFdLibDLL.BuildDataTiming(_canfdConfig.DataBitrate);
                    initConfig.workMode = (byte)_canfdConfig.Mode;
                    initConfig.res = 0;

                    // 配置过滤器(标准帧/扩展帧至少一组有效)
                    ApplyFilter(ref initConfig);

                    // 分配持久化收发缓冲区
                    _rxFrameSize = Marshal.SizeOf<ItekonCanFdLibDLL.ItekCanFdReceiveData>();
                    rxBuffer = Marshal.AllocHGlobal(_rxFrameSize * RxBatchSize);
                    txBuffer = Marshal.AllocHGlobal(Marshal.SizeOf<ItekonCanFdLibDLL.ItekCanFdTransmitData>());

                    // 初始化通道
                    pInitConfig = Marshal.AllocHGlobal(Marshal.SizeOf<ItekonCanFdLibDLL.ItekCanFdChannelInitConfig>());
                    Marshal.StructureToPtr(initConfig, pInitConfig, false);
                    chHandle = ItekonCanFdLibDLL.iTek_InitCAN(devHandle, channel, pInitConfig);
                    if (chHandle == IntPtr.Zero) throw new Exception("Initialize can channel failed");

                    // 启动通道
                    if (ItekonCanFdLibDLL.iTek_StartCAN(chHandle) == 0)
                        throw new Exception("Start can channel failed");

                    // 所有操作成功后，才提交状态
                    _openedDeviceType = deviceType;
                    _openedDeviceIndex = deviceIndex;
                    _openedChannel = channel;
                    _openedChannelHandle = chHandle;
                    //官方 C#/Qt 示例：USB 与 NET 的收发/清缓存均使用 InitCAN 返回的通道句柄
                    _openedIoHandle = chHandle;
                    _openedProtocol = protocol;
                    _rxBuffer = rxBuffer;
                    _txBuffer = txBuffer;
                    chHandle = IntPtr.Zero;
                    rxBuffer = IntPtr.Zero;
                    txBuffer = IntPtr.Zero;

                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (rxBuffer != IntPtr.Zero) Marshal.FreeHGlobal(rxBuffer);
                    if (txBuffer != IntPtr.Zero) Marshal.FreeHGlobal(txBuffer);
                    FreeBuffers();
                    if (chHandle != IntPtr.Zero) { try { ItekonCanFdLibDLL.iTek_ResetCAN(chHandle); } catch { } }
                    if (devHandle != IntPtr.Zero) { try { ItekonCanFdLibDLL.CloseDevice(deviceType, deviceIndex); } catch { } }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _isOpen = false;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
                finally
                {
                    if (pInitConfig != IntPtr.Zero) Marshal.FreeHGlobal(pInitConfig);
                }
            }
        }

        private void ApplyFilter(ref ItekonCanFdLibDLL.ItekCanFdChannelInitConfig initConfig)
        {
            if (_canfdConfig.EnableFilter)
            {
                if (_canfdConfig.ExtendedFilter)
                {
                    initConfig.Standard.num = 0;
                    initConfig.Extend.num = 1;
                    initConfig.Extend.filterDataExtend[0] = new ItekonCanFdLibDLL.FilterData
                    {
                        frameType = 1,
                        filterType = 0,     //范围 ID
                        ID1 = _canfdConfig.Filter_FromId,
                        ID2 = _canfdConfig.Filter_ToId,
                    };
                }
                else
                {
                    initConfig.Extend.num = 0;
                    initConfig.Standard.num = 1;
                    initConfig.Standard.filterDataStandard[0] = new ItekonCanFdLibDLL.FilterData
                    {
                        frameType = 0,
                        filterType = 0,     //范围 ID
                        ID1 = _canfdConfig.Filter_FromId,
                        ID2 = _canfdConfig.Filter_ToId,
                    };
                }
            }
            else
            {
                // 不使用过滤时也必须给出一组接收全部 ID 的过滤规则
                initConfig.Standard.num = 1;
                initConfig.Standard.filterDataStandard[0] = new ItekonCanFdLibDLL.FilterData
                {
                    frameType = 0,
                    filterType = 0,
                    ID1 = 0,
                    ID2 = CanFrameDef.CAN_STD_MAX_ID_FLAG,
                };
                initConfig.Extend.num = 1;
                initConfig.Extend.filterDataExtend[0] = new ItekonCanFdLibDLL.FilterData
                {
                    frameType = 1,
                    filterType = 0,
                    ID1 = 0,
                    ID2 = CanFrameDef.CAN_ID_FLAG,
                };
            }
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
                    // 连续读取直到无数据
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
                if (!_isOpen || _openedIoHandle == IntPtr.Zero || _rxBuffer == IntPtr.Zero) return false;

                try
                {
                    // 非阻塞批量读取(wait_time=0),单次调用最多取 RxBatchSize 帧
                    uint receiveNum = ItekonCanFdLibDLL.iTek_Receive(
                        _openedIoHandle, _rxBuffer, RxBatchSize, 0);

                    // 0xFFFFFFFF(-1) 表示读取失败(设备掉线/错误),按无数据处理
                    if (receiveNum == 0 || receiveNum == 0xFFFFFFFF) return false;

                    int count = (int)Math.Min(receiveNum, (uint)RxBatchSize);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr pFrame = IntPtr.Add(_rxBuffer, i * _rxFrameSize);
                        var recData = Marshal.PtrToStructure<ItekonCanFdLibDLL.ItekCanFdReceiveData>(pFrame);

                        CANFrame? frame = ParseFrame(recData);
                        if (frame != null) eventHandler?.Invoke(this, frame);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Read)}:{ex.Message}");
                }
            }
        }

        private static CANFrame? ParseFrame(ItekonCanFdLibDLL.ItekCanFdReceiveData recData)
        {
            var canfdFrame = recData.frame;
            if (canfdFrame.data == null) return null;

            bool isExtended = (canfdFrame.can_id & 0x40000000U) != 0;
            bool isRtr = (canfdFrame.can_id & 0x20000000U) != 0;

            //转换为工程约定的 SocketCAN 风格 ID
            long id = canfdFrame.can_id & CanFrameDef.CAN_ID_FLAG;
            if (isExtended) id |= CanFrameDef.CAN_EFF_FLAG;
            if (isRtr) id |= CanFrameDef.CAN_RTR_FLAG;

            //接收帧可能带 0x40 方向位,先取低 2 位帧类型
            byte frameCanType = (byte)(canfdFrame.cantype & ItekonCanFdFlagParser.CAN_TYPE_MASK);
            bool isFd = frameCanType == ItekonCanFdFlagParser.CAN_TYPE_CANFD
                     || frameCanType == ItekonCanFdFlagParser.CAN_TYPE_CANFD_BRS;
            int dataLen = Math.Min((int)canfdFrame.len,
                isFd ? DataLenConverter.MaxCanFdDataLength : DataLenConverter.MaxCanDataLength);

            CANFrame frame = new CANFrame
            {
                ID = id,
                DLC = dataLen,
                Flag = (int)canfdFrame.can_id,
                Flags = ItekonCanFdFlagParser.Parse(canfdFrame.can_id, canfdFrame.flags, frameCanType),
                //iTek 时间戳单位为微秒
                Timestamp = recData.timestamp,
                FDF = isFd,
                BRS = frameCanType == ItekonCanFdFlagParser.CAN_TYPE_CANFD_BRS,
                Data = new byte[dataLen],
                Dir = 1,
            };

            if (dataLen > 0)
                Array.Copy(canfdFrame.data, 0, frame.Data, 0, dataLen);

            return frame;
        }

        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _openedIoHandle == IntPtr.Zero || _txBuffer == IntPtr.Zero) return false;
                if (frame is not CANFrame canFrame) return false;

                try
                {
                    return SendCore(canFrame);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }

        private bool SendCore(CANFrame canFrame)
        {
            if (canFrame.BRS && !canFrame.FDF)
                throw new ArgumentException("BRS is only valid for CANFD frame(FDF must be set).");

            if (canFrame.FDF)
            {
                if (_openedProtocol == ItekonCanFdProtocol.CAN)
                    throw new ArgumentException("Classic CAN channel does not support CANFD frame(FDF).");
                if (!DataLenConverter.IsValidCanFdLength(canFrame.DLC))
                    throw new ArgumentException($"Invalid CANFD DLC({canFrame.DLC}), valid lengths: 0-8,12,16,20,24,32,48,64.");
            }
            else
            {
                if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
                    throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");
            }

            bool isRtr = CanFrameHelper.IsRTR(canFrame.ID);
            if (!isRtr && (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC))
                throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

            canFrame.Dir = 0;
            uint validId = CanFrameHelper.GetId(canFrame.ID);
            bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);

            //逻辑帧类型:0=CAN,2=CANFD,3=CANFD 加速
            byte frameCanType = canFrame.FDF
                ? (canFrame.BRS ? ItekonCanFdFlagParser.CAN_TYPE_CANFD_BRS : ItekonCanFdFlagParser.CAN_TYPE_CANFD)
                : ItekonCanFdFlagParser.CAN_TYPE_CAN;

            ItekonCanFdLibDLL.ItekCanFdTransmitData sendData = new ItekonCanFdLibDLL.ItekCanFdTransmitData
            {
                frame = new ItekonCanFdLibDLL.canfd_frame
                {
                    can_id = ItekonCanFdLibDLL.MakeCanId(validId, isExtended, isRtr),
                    len = (byte)canFrame.DLC,
                    flags = 0,
                    sendType = 0,
                    //发送方向 cantype 需要置 0x40 标志
                    cantype = (byte)(ItekonCanFdLibDLL.TX_FLAG | frameCanType),
                    data = new byte[DataLenConverter.MaxCanFdDataLength],
                },
                send_type = (ushort)_canfdConfig.SendType,
            };

            if (!isRtr && canFrame.DLC > 0)
                Array.Copy(canFrame.Data, 0, sendData.frame.data, 0, canFrame.DLC);

            Marshal.StructureToPtr(sendData, _txBuffer, true);
            uint result = ItekonCanFdLibDLL.iTek_Transmit(_openedIoHandle, _txBuffer, 1);
            if (result != 1) return false;

            canFrame.Flags = ItekonCanFdFlagParser.Parse(
                sendData.frame.can_id, sendData.frame.flags, frameCanType);
            eventHandler?.Invoke(this, canFrame);
            return true;
        }
    }
}
