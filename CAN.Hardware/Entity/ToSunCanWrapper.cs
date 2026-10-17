using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Tosun, TosunDeviceType.Generic, CommProtocol.CAN)]
    public class ToSunCanWrapper : IHardware
    {
        //同星设备句柄,IntPtr.Zero 表示未打开
        private IntPtr _handle = IntPtr.Zero;
        private readonly ToSunCANConfig _canConfig = new ToSunCANConfig();
        private uint _status = 0;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _canConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();

        // 事件回调委托需保持引用，防止被GC回收
        private TCANQueueEvent_Win32? _canEventCallback = null;

        private bool _isOpen = false;
        public bool Close()
        {
            lock (_lock)
            {
                if (!_isOpen) return true;
                _isOpen = false;

                // 先注销事件回调
                if (_handle != IntPtr.Zero && _canEventCallback != null)
                {
                    try { ToSunCanLibDLL.tscan_unregister_event_can(_handle, _canEventCallback); } catch { }
                    _canEventCallback = null;
                }

                // 断开设备(库级+设备级引用计数,仅最后一个实例真正断开)
                if (_handle != IntPtr.Zero)
                {
                    try { ToSunCanLibDLL.DisconnectDevice(); } catch { }
                    _handle = IntPtr.Zero;
                }
            }
            return true;
        }
        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (_handle != IntPtr.Zero) ToSunCanLibDLL.tsfifo_clear_can_receive_buffers(_handle, _canConfig.ChannelIndex);
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

                try
                {
                    // 连接设备(内部完成库初始化,多实例共享设备句柄)
                    _status = ToSunCanLibDLL.ConnectDevice(null, _canConfig.EnableFIFO, _canConfig.EnableErrorFrame, _canConfig.EnableTurbe, out _handle);
                    if (_status != 0) throw new Exception($"Connect device failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

                    // 设置波特率
                    _status = ToSunCanLibDLL.tscan_config_can_by_baudrate(_handle, _canConfig.ChannelIndex, _canConfig.CanBitrate, _canConfig.EnableA120Ohm ? 1u : 0u);
                    if (_status != 0) throw new Exception($"Set can baudrate failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

                    // 设置过滤器(逐 ID 白名单)
                    if (_canConfig.EnableFilter && _canConfig.FilterIDArray != null)
                    {
                        foreach (uint id in _canConfig.FilterIDArray)
                        {
                            _status = ToSunCanLibDLL.tsfifo_add_can_canfd_pass_filter(_handle, _canConfig.ChannelIndex, (int)id, id <= CanFrameDef.CAN_STD_MAX_ID_FLAG);
                            if (_status != 0) throw new Exception($"Set frame filter failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");
                        }
                    }

                    // 注册接收事件回调
                    _canEventCallback = OnCanEvent;
                    _status = ToSunCanLibDLL.tscan_register_event_can(_handle, _canEventCallback);
                    if (_status != 0) throw new Exception($"Register can event failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

                    // 所有操作成功后，才提交状态
                    _isOpen = true;
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (_handle != IntPtr.Zero && _canEventCallback != null)
                    {
                        try { ToSunCanLibDLL.tscan_unregister_event_can(_handle, _canEventCallback); } catch { }
                        _canEventCallback = null;
                    }
                    if (_handle != IntPtr.Zero)
                    {
                        try { ToSunCanLibDLL.DisconnectDevice(); } catch { }
                        _handle = IntPtr.Zero;
                    }
                    _isOpen = false;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }
        private void OnCanEvent(ref TLIBCAN canData)
        {
            // 回调运行在驱动线程,异常不能向外抛
            try
            {
                if (!_isOpen) return;
                // 事件回调是设备级的,仅处理本通道的报文
                if (canData.FIdxChn != (byte)_canConfig.ChannelIndex) return;
                // 丢弃 TX 帧(发送回显由 Send 成功后自行触发)
                if ((canData.FProperties & 0x01) != 0) return;

                CANFrame frame = new CANFrame();
                //将 EFF/RTR/ERR 标志合成到 ID 高位(SocketCAN 风格)
                long id = canData.FIdentifier & CanFrameDef.CAN_ID_FLAG;
                if ((canData.FProperties & 0x04) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                if ((canData.FProperties & 0x02) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
                if ((canData.FProperties & 0x80) != 0) id |= CanFrameDef.CAN_ERR_FLAG;
                frame.ID = id;
                //钳制dlc到经典CAN有效范围，防止驱动返回异常值导致越界
                frame.DLC = Math.Min((int)canData.FDLC, DataLenConverter.MaxCanDataLength);
                frame.Flag = canData.FProperties;
                frame.Flags = ToSunCanFlagParser.Parse(canData.FProperties);
                frame.Timestamp = canData.FTimeUS;
                frame.Data = new byte[frame.DLC];
                frame.Dir = 1;
                Array.Copy(canData.FData, 0, frame.Data, 0, frame.DLC);

                eventHandler?.Invoke(this, frame);
            }
            catch (Exception) { }
        }
        public bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _handle == IntPtr.Zero) return false;

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
                    var validId = CanFrameHelper.GetId(canFrame.ID);
                    var isExtended = CanFrameHelper.IsEFF(canFrame.ID);
                    var isRTR = CanFrameHelper.IsRTR(canFrame.ID);
                    //TLIBCAN.FData 为固定 8 字节数组,需按 SizeConst 分配
                    byte[] bytes = new byte[DataLenConverter.MaxCanDataLength];
                    Array.Copy(canFrame.Data, 0, bytes, 0, canFrame.DLC);
                    TLIBCAN canMsg = new TLIBCAN(_canConfig.ChannelIndex, (int)validId, true, isExtended, isRTR, (byte)canFrame.DLC, bytes);

                    _status = ToSunCanLibDLL.tscan_transmit_can_async(_handle, ref canMsg);
                    if (_status != 0) return false;
                    canFrame.Flags = ToSunCanFlagParser.Parse(canMsg.FProperties);
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
