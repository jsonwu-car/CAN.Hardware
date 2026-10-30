using System.Runtime.InteropServices;
using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Etas, EtasDeviceType.Generic, CommProtocol.CANFD)]
    public class EtasCanFdWrapper : IHardware
    {
        //BOA 句柄(UNDEFINED 表示未打开),控制器 + 发送队列 + 接收队列
        private int _ctrlHandle = EtasBoaLibDLL.UNDEFINED;
        private int _txHandle = EtasBoaLibDLL.UNDEFINED;
        private int _rxHandle = EtasBoaLibDLL.UNDEFINED;
        private readonly EtasCanFDConfig _canConfig = new EtasCanFDConfig();

        //接收回调委托必须持有引用,防止被 GC 回收后驱动回调野指针
        private EtasBoaLibDLL.OciCallbackEx? _rxCallback;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _canConfig;

        // 使用单一锁保护所有状态和硬件操作; _isOpen 会被驱动回调线程读取, 需要 volatile
        private readonly object _lock = new object();
        private volatile bool _isOpen = false;

        // 硬件定时器频率(tick/秒), Open 时查询; 0 表示查询失败, 报文时间戳保持 0
        private uint _tickFrequency;

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                int ctrl = EtasBoaLibDLL.UNDEFINED;
                try
                {
                    // 定位并加载 dll-ocdProxy.dll / dll-csiBind.dll(注册表/默认路径 + PATH 追加)
                    EtasBoaLibDLL.EnsureLoaded();

                    string uri = ResolveUri();

                    // 创建控制器(内部自行 CSI 搜索),API 版本固定协商 1.3
                    // out 句柄先接局部变量: 失败时驱动不回写句柄, 编组默认值为 0, 不能让它混进清理路径
                    var version = EtasBoaLibDLL.ApiV13;
                    var status = EtasBoaLibDLL.CreateCANControllerVersion(uri, ref version, out int created);
                    if (status != EtasBoaLibDLL.EtasErrorCode.SUCCESS)
                        throw new Exception($"Create CAN controller failed({uri}, {status})");
                    ctrl = created;

                    // 配置并打开控制器(CAN FD: canFDEnabled=1 + 数据段波特率);仅把带 ERROR 位的返回值当失败,WARNING 放行
                    // 自定义波特率: 仲裁段/数据段各自 Hz 直填,连同各自采样点/BTL_Cycles/SJW 一起下发
                    var cfg = _canConfig.EnableCustomBitrate
                        ? EtasBoaLibDLL.EtasCanConfiguration.Create(_canConfig.CustomArbitrationBitrate, _canConfig.CustomDataBitrate,
                            _canConfig.ArbitrationSamplePoint, _canConfig.ArbitrationBtlCycles, _canConfig.ArbitrationSjw,
                            _canConfig.DataSamplePoint, _canConfig.DataBtlCycles, _canConfig.DataSjw)
                        : EtasBoaLibDLL.EtasCanConfiguration.Create((uint)_canConfig.CanFdArbitrationBitrate, (uint)_canConfig.CanFdDataBitrate);
                    var props = new EtasBoaLibDLL.EtasControllerProperties { mode = 0 };
                    status = EtasBoaLibDLL.OpenCANController(ctrl, ref cfg, ref props);
                    if (EtasBoaLibDLL.IsError(status))
                        throw new Exception($"Open CAN controller failed({uri}, {status})");

                    // 查询硬件定时器频率供报文时间戳换算; 失败降级为无时间戳, 不影响打开
                    _tickFrequency = EtasBoaLibDLL.QueryTickFrequency(ctrl);

                    // 创建发送队列
                    var txCfg = new EtasBoaLibDLL.EtasTxQueueConfiguration();
                    status = EtasBoaLibDLL.CreateCANTxQueue(ctrl, ref txCfg, out _txHandle);
                    if (status != EtasBoaLibDLL.EtasErrorCode.SUCCESS)
                        throw new Exception($"Create CAN TxQueue failed({status})");

                    // 创建接收队列(回调驱动,驱动线程直接推送报文)
                    _rxCallback = OnRxMsg;
                    var rxCfg = new EtasBoaLibDLL.EtasRxQueueConfiguration
                    {
                        onFrame = new EtasBoaLibDLL.EtasRxCallbackSingleMsg { functionEx = _rxCallback, userData = IntPtr.Zero },
                    };
                    status = EtasBoaLibDLL.CreateCANRxQueue(ctrl, ref rxCfg, out _rxHandle);
                    if (status != EtasBoaLibDLL.EtasErrorCode.SUCCESS)
                        throw new Exception($"Create CAN RxQueue failed({status})");

                    // 全 0 过滤器 = 放行所有帧
                    var filters = new EtasBoaLibDLL.EtasRxFilter[] { new EtasBoaLibDLL.EtasRxFilter() };
                    status = EtasBoaLibDLL.AddCANFrameFilter(_rxHandle, filters, (uint)filters.Length);
                    if (status != EtasBoaLibDLL.EtasErrorCode.SUCCESS)
                        throw new Exception($"Add CAN frame filter failed({status})");

                    _ctrlHandle = ctrl;
                    _isOpen = true;
                    return true;
                }
                catch (Exception ex)
                {
                    // 控制器已创建则级联销毁(队列随控制器释放)
                    if (ctrl != EtasBoaLibDLL.UNDEFINED)
                    {
                        try { EtasBoaLibDLL.DestroyCANController(ctrl); } catch { }
                    }
                    _ctrlHandle = _txHandle = _rxHandle = EtasBoaLibDLL.UNDEFINED;
                    _tickFrequency = 0;
                    _isOpen = false;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        // 解析硬件 URI: 配置了 InterfaceUri 直接使用,否则按 ChannelIndex 从 CSI 枚举结果中取
        private string ResolveUri()
        {
            string uri = _canConfig.InterfaceUri;
            if (string.IsNullOrWhiteSpace(uri))
            {
                var nodes = EtasBoaLibDLL.EnumerateNodes("CAN:");
                if (nodes.Count == 0)
                    throw new Exception("未发现任何 ETAS CAN 硬件节点");
                if (_canConfig.ChannelIndex < 0 || _canConfig.ChannelIndex >= nodes.Count)
                    throw new Exception($"通道索引 {_canConfig.ChannelIndex} 超出可用 CAN 节点数({nodes.Count})");
                uri = nodes[_canConfig.ChannelIndex];
            }
            return uri.StartsWith(EtasBoaLibDLL.URI_PREFIX, StringComparison.Ordinal) ? uri : EtasBoaLibDLL.URI_PREFIX + uri;
        }

        public bool Close()
        {
            lock (_lock)
            {
                if (!_isOpen) return true;

                // 先置关闭标志, 阻止在途回调继续推送报文
                _isOpen = false;

                bool ok = true;
                if (_ctrlHandle != EtasBoaLibDLL.UNDEFINED)
                {
                    // 先 Close 让控制器下线, 再 Destroy 级联释放其下的 Tx/Rx 队列
                    try { EtasBoaLibDLL.CloseCANController(_ctrlHandle); } catch { }
                    try
                    {
                        ok = !EtasBoaLibDLL.IsError(EtasBoaLibDLL.DestroyCANController(_ctrlHandle));
                    }
                    catch { ok = false; }
                    if (ok) _ctrlHandle = _txHandle = _rxHandle = EtasBoaLibDLL.UNDEFINED;
                }
                // 接收委托不置空: 驱动侧可能仍有在途回调, 保持引用防止 GC 回收 thunk 后命中野指针
                return ok;
            }
        }

        public void FlushReceiveBuffer()
        {
            // 当前实现的 OCI 子集无队列清空接口,留空
        }

        public void FlushTransmitBuffer()
        {
            // 当前实现的 OCI 子集无队列清空接口,留空
        }

        // OCI 接收回调: 在驱动线程上执行,先读消息头 type 再按类型重解结构体
        private void OnRxMsg(IntPtr userData, IntPtr msg)
        {
            if (!_isOpen || msg == IntPtr.Zero) return;

            try
            {
                uint msgType = (uint)Marshal.ReadInt32(msg);
                CANFrame frame1;
                switch (msgType)
                {
                    case EtasBoaLibDLL.MSG_TYPE_RX_MESSAGE:
                        frame1 = Convert(Marshal.PtrToStructure<EtasBoaLibDLL.EtasCanRxMsg>(msg));
                        break;
                    case EtasBoaLibDLL.MSG_TYPE_FDRX_MESSAGE:
                        frame1 = Convert(Marshal.PtrToStructure<EtasBoaLibDLL.EtasCanFdRxMsg>(msg));
                        break;
                    default:
                        // 错误帧/总线事件/队列事件等暂不处理
                        return;
                }
                eventHandler?.Invoke(this, frame1);
            }
            catch
            {
                // 回调在驱动线程上执行,异常不能抛回 native 侧
            }
        }

        private CANFrame Convert(in EtasBoaLibDLL.EtasCanRxMsg rx)
        {
            CANFrame frame1 = new CANFrame();
            frame1.ID = BuildId(rx.frameID, rx.flags);
            //钳制dlc到经典CAN有效范围，防止驱动返回异常值导致越界
            frame1.DLC = Math.Min((int)rx.dlc, DataLenConverter.MaxCanDataLength);
            frame1.Flag = rx.flags;
            frame1.Flags = EtasCanFlagParser.Parse(rx.flags);
            frame1.Timestamp = TicksToMicros(rx.timeStamp);
            frame1.Data = new byte[frame1.DLC];
            frame1.Dir = 1;
            Array.Copy(rx.data, 0, frame1.Data, 0, frame1.DLC);
            return frame1;
        }

        private CANFrame Convert(in EtasBoaLibDLL.EtasCanFdRxMsg rx)
        {
            CANFrame frame1 = new CANFrame();
            frame1.ID = BuildId(rx.frameID, rx.flags);
            //钳制负载长度到 CANFD 有效范围，防止驱动返回异常值导致越界
            frame1.DLC = Math.Min((int)rx.dlc, DataLenConverter.MaxCanFdDataLength);
            frame1.Flag = rx.flags;
            frame1.Flags = EtasCanFlagParser.Parse(rx.flags);
            frame1.FDF = (rx.flags & EtasBoaLibDLL.MSG_FLAG_FD_DATA) != 0;
            frame1.BRS = (rx.flags & EtasBoaLibDLL.MSG_FLAG_FD_DATA_BIT_RATE) != 0;
            frame1.Timestamp = TicksToMicros(rx.timeStamp);
            frame1.Data = new byte[frame1.DLC];
            frame1.Dir = 1;
            Array.Copy(rx.data, 0, frame1.Data, 0, frame1.DLC);
            return frame1;
        }

        //硬件 tick 换算为微秒(与 Peak/ToSun/Vector/Zlg 通路单位一致); 拆商余计算避免 tick×1e6 溢出
        private ulong TicksToMicros(long ticks)
        {
            if (_tickFrequency == 0 || ticks <= 0) return 0;
            ulong t = (ulong)ticks, f = _tickFrequency;
            return t / f * 1_000_000UL + t % f * 1_000_000UL / f;
        }

        //将 EFF/RTR 标志合成到 ID 高位(SocketCAN 风格)
        private static long BuildId(uint frameID, ushort flags)
        {
            long id = frameID;
            if ((flags & EtasBoaLibDLL.MSG_FLAG_EXTENDED) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
            if ((flags & EtasBoaLibDLL.MSG_FLAG_REMOTE_FRAME) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
            return id;
        }

        public bool Send(FrameBase frame)
        {
            if (frame is not CANFrame canFrame) return false;

            //发送前校验数据长度合法性（非法参数直接抛异常，不做静默失败）
            if (canFrame.BRS && !canFrame.FDF)
                throw new ArgumentException("BRS requires FDF(CANFD frame).");
            if (canFrame.FDF && CanFrameHelper.IsRTR(canFrame.ID))
                throw new ArgumentException("CANFD frame does not support RTR.");
            if (canFrame.FDF)
            {
                if (!DataLenConverter.IsValidCanFdLength(canFrame.DLC))
                    throw new ArgumentException($"Invalid CANFD data length({canFrame.DLC}), valid lengths: 0-8,12,16,20,24,32,48,64.");
            }
            else if (!DataLenConverter.IsValidCanLength(canFrame.DLC))
            {
                throw new ArgumentException($"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");
            }
            if (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC)
                throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

            ushort flags;
            bool sent;
            lock (_lock)
            {
                if (!_isOpen || _txHandle == EtasBoaLibDLL.UNDEFINED) return false;

                try
                {
                    canFrame.Dir = 0;
                    uint frameID = CanFrameHelper.GetId(canFrame.ID);
                    flags = 0;
                    if (CanFrameHelper.IsEFF(canFrame.ID)) flags |= EtasBoaLibDLL.MSG_FLAG_EXTENDED;

                    //按帧形态选消息结构(均带 type+reserved 头): FD 帧用 FDTX_MESSAGE, 经典帧即使在 FD 通道也用 TX_MESSAGE
                    int msgSize;
                    Action<IntPtr> marshal;
                    if (canFrame.FDF)
                    {
                        flags |= EtasBoaLibDLL.MSG_FLAG_FD_DATA;
                        if (canFrame.BRS) flags |= EtasBoaLibDLL.MSG_FLAG_FD_DATA_BIT_RATE;
                        var fdMsg = new EtasBoaLibDLL.EtasCanFdTxMsg
                        {
                            type = EtasBoaLibDLL.MSG_TYPE_FDTX_MESSAGE,
                            reserved = 0,
                            frameID = frameID,
                            flags = flags,
                            res = 0,
                            size = (byte)canFrame.DLC,
                            data = new byte[DataLenConverter.MaxCanFdDataLength],
                        };
                        Array.Copy(canFrame.Data, 0, fdMsg.data, 0, canFrame.DLC);
                        msgSize = EtasBoaLibDLL.FD_TX_MSG_SIZE;
                        marshal = p => Marshal.StructureToPtr(fdMsg, p, false);
                    }
                    else
                    {
                        if (CanFrameHelper.IsRTR(canFrame.ID)) flags |= EtasBoaLibDLL.MSG_FLAG_REMOTE_FRAME;
                        var msg = new EtasBoaLibDLL.EtasCanTxMsg
                        {
                            type = EtasBoaLibDLL.MSG_TYPE_TX_MESSAGE,
                            reserved = 0,
                            frameID = frameID,
                            flags = flags,
                            res = 0,
                            dlc = (byte)canFrame.DLC,
                            data = new byte[DataLenConverter.MaxCanDataLength],
                        };
                        Array.Copy(canFrame.Data, 0, msg.data, 0, canFrame.DLC);
                        msgSize = EtasBoaLibDLL.TX_MSG_SIZE;
                        marshal = p => Marshal.StructureToPtr(msg, p, false);
                    }

                    // OCI_WriteCANDataEx 收发 OCI_CANMessageEx* 指针数组(支持混合尺寸批量写),这里单帧;
                    // timeout=0 立即返回,队列满时由 remaining 回读实际未写入数量
                    IntPtr ptr = Marshal.AllocHGlobal(msgSize);
                    try
                    {
                        marshal(ptr);
                        var messages = new IntPtr[] { ptr };
                        uint remaining = 1;
                        var status = EtasBoaLibDLL.WriteCANDataEx(_txHandle, 0, messages, 1, ref remaining);
                        sent = status == EtasBoaLibDLL.EtasErrorCode.SUCCESS && remaining == 0;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}", ex);
                }
            }

            if (!sent) return false;

            // 锁外推送发送回显, 避免订阅者在持锁路径上阻塞或重入
            canFrame.Flag = flags;
            canFrame.Flags = EtasCanFlagParser.Parse(flags);
            eventHandler?.Invoke(this, canFrame);
            return true;
        }
    }
}
