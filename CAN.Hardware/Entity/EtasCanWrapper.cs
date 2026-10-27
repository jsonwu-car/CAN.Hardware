using System.Runtime.InteropServices;
using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Etas, EtasDeviceType.Generic, CommProtocol.CAN)]
    public class EtasCanWrapper : IHardware
    {
        //BOA 句柄(UNDEFINED 表示未打开),控制器 + 发送队列 + 接收队列
        private int _ctrlHandle = EtasBoaLibDLL.UNDEFINED;
        private int _txHandle = EtasBoaLibDLL.UNDEFINED;
        private int _rxHandle = EtasBoaLibDLL.UNDEFINED;
        private readonly EtasCANConfig _canConfig = new EtasCANConfig();

        //接收回调委托必须持有引用,防止被 GC 回收后驱动回调野指针
        private EtasBoaLibDLL.OciCallbackEx? _rxCallback;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _canConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();
        private bool _isOpen = false;

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
                    var version = EtasBoaLibDLL.ApiV13;
                    var status = EtasBoaLibDLL.CreateCANControllerVersion(uri, ref version, out ctrl);
                    if (status != EtasBoaLibDLL.EtasErrorCode.SUCCESS)
                        throw new Exception($"Create CAN controller failed({uri}, {status})");

                    // 配置并打开控制器(经典 CAN);仅把带 ERROR 位的返回值当失败,WARNING(如 PARAM_ADAPTED 参数被微调)放行
                    var cfg = EtasBoaLibDLL.EtasCanConfiguration.Create((uint)_canConfig.CanBitrate, 0);
                    var props = new EtasBoaLibDLL.EtasControllerProperties { mode = 0 };
                    status = EtasBoaLibDLL.OpenCANController(ctrl, ref cfg, ref props);
                    if (EtasBoaLibDLL.IsError(status))
                        throw new Exception($"Open CAN controller failed({uri}, {status})");

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
                if (_canConfig.ChannelIndex >= nodes.Count)
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

                // Destroy 会级联释放其下的 Tx/Rx 队列
                if (_ctrlHandle != EtasBoaLibDLL.UNDEFINED)
                {
                    try { EtasBoaLibDLL.DestroyCANController(_ctrlHandle); } catch { }
                    _ctrlHandle = EtasBoaLibDLL.UNDEFINED;
                }
                _txHandle = _rxHandle = EtasBoaLibDLL.UNDEFINED;
                _rxCallback = null;
                _isOpen = false;
            }
            return true;
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

        private static CANFrame Convert(in EtasBoaLibDLL.EtasCanRxMsg rx)
        {
            CANFrame frame1 = new CANFrame();
            frame1.ID = BuildId(rx.frameID, rx.flags);
            //钳制dlc到经典CAN有效范围，防止驱动返回异常值导致越界
            frame1.DLC = Math.Min((int)rx.dlc, DataLenConverter.MaxCanDataLength);
            frame1.Flag = rx.flags;
            frame1.Flags = EtasCanFlagParser.Parse(rx.flags);
            //ETAS 时间戳为硬件 timer tick(频率需 OCI_GetTimerCapabilities 查询),当前未换算
            frame1.Data = new byte[frame1.DLC];
            frame1.Dir = 1;
            Array.Copy(rx.data, 0, frame1.Data, 0, frame1.DLC);
            return frame1;
        }

        private static CANFrame Convert(in EtasBoaLibDLL.EtasCanFdRxMsg rx)
        {
            CANFrame frame1 = new CANFrame();
            frame1.ID = BuildId(rx.frameID, rx.flags);
            frame1.DLC = Math.Min((int)rx.dlc, DataLenConverter.MaxCanFdDataLength);
            frame1.Flag = rx.flags;
            frame1.Flags = EtasCanFlagParser.Parse(rx.flags);
            frame1.FDF = (rx.flags & EtasBoaLibDLL.MSG_FLAG_FD_DATA) != 0;
            frame1.BRS = (rx.flags & EtasBoaLibDLL.MSG_FLAG_FD_DATA_BIT_RATE) != 0;
            frame1.Data = new byte[frame1.DLC];
            frame1.Dir = 1;
            Array.Copy(rx.data, 0, frame1.Data, 0, frame1.DLC);
            return frame1;
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
            lock (_lock)
            {
                if (!_isOpen || _txHandle == EtasBoaLibDLL.UNDEFINED) return false;

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
                    var msg = new EtasBoaLibDLL.EtasCanTxMsg
                    {
                        frameID = CanFrameHelper.GetId(canFrame.ID),
                        flags = 0,
                        res = 0,
                        dlc = (byte)canFrame.DLC,
                        data = new byte[DataLenConverter.MaxCanDataLength],
                    };
                    if (CanFrameHelper.IsEFF(canFrame.ID)) msg.flags |= EtasBoaLibDLL.MSG_FLAG_EXTENDED;
                    if (CanFrameHelper.IsRTR(canFrame.ID)) msg.flags |= EtasBoaLibDLL.MSG_FLAG_REMOTE_FRAME;
                    Array.Copy(canFrame.Data, 0, msg.data, 0, canFrame.DLC);

                    // OCI_WriteCANDataEx 收发指针数组(支持混合尺寸批量写),这里单帧;
                    // timeout=0 立即返回,队列满时由 remaining 回读实际未写入数量
                    IntPtr ptr = Marshal.AllocHGlobal(EtasBoaLibDLL.TX_MSG_SIZE);
                    try
                    {
                        Marshal.StructureToPtr(msg, ptr, false);
                        var messages = new IntPtr[] { ptr };
                        uint remaining = 1;
                        var status = EtasBoaLibDLL.WriteCANDataEx(_txHandle, 0, messages, 1, ref remaining);
                        if (status != EtasBoaLibDLL.EtasErrorCode.SUCCESS || remaining != 0) return false;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }

                    canFrame.Flag = msg.flags;
                    canFrame.Flags = EtasCanFlagParser.Parse(msg.flags);
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
