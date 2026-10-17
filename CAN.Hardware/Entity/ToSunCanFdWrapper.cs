using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Tosun, TosunDeviceType.Generic, CommProtocol.CANFD)]
    public class ToSunCanFdWrapper : IHardware
    {
        //同星设备句柄,IntPtr.Zero 表示未打开
        private IntPtr _handle = IntPtr.Zero;
        private readonly ToSunCanFDConfig _canfdConfig = new ToSunCanFDConfig();
        private uint _status = 0;

        //用于对外推送报文
        public EventHandler<FrameBase>? eventHandler { get; set; }

        //用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        public BusConfigBase Config => _canfdConfig;

        // 使用单一锁保护所有状态和硬件操作
        private readonly object _lock = new object();

        // 事件回调委托需保持引用，防止被GC回收
        private TCANFDQueueEvent_Win32? _canFdEventCallback = null;

        private bool _isOpen = false;
        public bool Close()
        {
            lock (_lock)
            {
                if (!_isOpen) return true;
                _isOpen = false;

                // 先注销事件回调
                if (_handle != IntPtr.Zero && _canFdEventCallback != null)
                {
                    try { ToSunCanLibDLL.tscan_unregister_event_canfd(_handle, _canFdEventCallback); } catch { }
                    _canFdEventCallback = null;
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
                if (_handle != IntPtr.Zero) ToSunCanLibDLL.tsfifo_clear_canfd_receive_buffers(_handle, _canfdConfig.ChannelIndex);
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
                    _status = ToSunCanLibDLL.ConnectDevice(null, _canfdConfig.EnableFIFO, _canfdConfig.EnableErrorFrame, _canfdConfig.EnableTurbe, out _handle);
                    if (_status != 0) throw new Exception($"Connect device failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

                    // 设置波特率
                    if (_canfdConfig.EnableCustomBitrate)
                    {
                        // 寄存器级自定义波特率配置
                        _status = ToSunCanLibDLL.tscan_configure_canfd_regs(_handle, _canfdConfig.ChannelIndex,
                            _canfdConfig.CanFdArbitrationBitrate, _canfdConfig.Arbitration_phase1, _canfdConfig.Arbitration_phase2, _canfdConfig.Arbitration_prescale, _canfdConfig.Arbitration_sjw,
                            _canfdConfig.CanFdDataBitrate, _canfdConfig.Data_phase1, _canfdConfig.Data_phase2, _canfdConfig.Data_prescale, _canfdConfig.Data_sjw,
                            _canfdConfig.ControllerType, _canfdConfig.ControllerMode, _canfdConfig.EnableA120Ohm);
                    }
                    else
                    {
                        _status = ToSunCanLibDLL.tscan_config_canfd_by_baudrate(_handle, _canfdConfig.ChannelIndex,
                            _canfdConfig.CanFdArbitrationBitrate, _canfdConfig.CanFdDataBitrate,
                            _canfdConfig.ControllerType, _canfdConfig.ControllerMode, _canfdConfig.EnableA120Ohm ? 1u : 0u);
                    }
                    if (_status != 0) throw new Exception($"Set canfd baudrate failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

                    // 设置过滤器(逐 ID 白名单,ID<=0x7FF 为标准帧)
                    if (_canfdConfig.EnableFilter && _canfdConfig.FilterIDArray != null)
                    {
                        foreach (uint id in _canfdConfig.FilterIDArray)
                        {
                            _status = ToSunCanLibDLL.tsfifo_add_can_canfd_pass_filter(_handle, _canfdConfig.ChannelIndex, (int)id, id <= CanFrameDef.CAN_STD_MAX_ID_FLAG);
                            if (_status != 0) throw new Exception($"Set frame filter failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");
                        }
                    }

                    // 注册接收事件回调
                    _canFdEventCallback = OnCanFdEvent;
                    _status = ToSunCanLibDLL.tscan_register_event_canfd(_handle, _canFdEventCallback);
                    if (_status != 0) throw new Exception($"Register canfd event failed({ToSunCanLibDLL.TSCAN_GetTSCANErrorDescription(_status)})");

                    // 所有操作成功后，才提交状态
                    _isOpen = true;
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (_handle != IntPtr.Zero && _canFdEventCallback != null)
                    {
                        try { ToSunCanLibDLL.tscan_unregister_event_canfd(_handle, _canFdEventCallback); } catch { }
                        _canFdEventCallback = null;
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
        private void OnCanFdEvent(ref TLIBCANFD canFdData)
        {
            // 回调运行在驱动线程,异常不能向外抛
            try
            {
                if (!_isOpen) return;
                // 事件回调是设备级的,仅处理本通道的报文
                if (canFdData.FIdxChn != (byte)_canfdConfig.ChannelIndex) return;
                // 丢弃 TX 帧(发送回显由 Send 成功后自行触发)
                if ((canFdData.FProperties & 0x01) != 0) return;

                CANFrame frame = new CANFrame();
                //将 EFF/RTR/ERR 标志合成到 ID 高位(SocketCAN 风格)
                long id = canFdData.FIdentifier & CanFrameDef.CAN_ID_FLAG;
                if ((canFdData.FProperties & 0x04) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                if ((canFdData.FProperties & 0x02) != 0) id |= CanFrameDef.CAN_RTR_FLAG;
                if ((canFdData.FProperties & 0x80) != 0) id |= CanFrameDef.CAN_ERR_FLAG;
                frame.ID = id;
                //DLC 编码换算为实际数据长度(经典帧 0-8 恒等映射,FD 帧 0-15 映射到 0-64)
                frame.DLC = DataLenConverter.DlcToLength(canFdData.FDLC);
                frame.Flag = canFdData.FProperties;
                frame.Flags = ToSunCanFlagParser.Parse(canFdData.FProperties, canFdData.FFDProperties);
                frame.Timestamp = canFdData.FTimeUS;
                frame.FDF = (canFdData.FFDProperties & 0x01) != 0;
                frame.BRS = (canFdData.FFDProperties & 0x02) != 0;
                frame.Data = new byte[frame.DLC];
                frame.Dir = 1;
                Array.Copy(canFdData.FData, 0, frame.Data, 0, frame.DLC);

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
                if (canFrame.BRS && !canFrame.FDF)
                    throw new ArgumentException("BRS is only valid for CANFD frame(FDF must be set).");
                bool validLength = canFrame.FDF
                    ? DataLenConverter.IsValidCanFdLength(canFrame.DLC)
                    : DataLenConverter.IsValidCanLength(canFrame.DLC);
                if (!validLength)
                    throw new ArgumentException(canFrame.FDF
                        ? $"Invalid CANFD DLC({canFrame.DLC}), valid lengths: 0-8,12,16,20,24,32,48,64."
                        : $"Invalid CAN DLC({canFrame.DLC}), valid range: 0-{DataLenConverter.MaxCanDataLength}.");
                if (canFrame.Data == null || canFrame.Data.Length < canFrame.DLC)
                    throw new ArgumentException($"Data length({canFrame.Data?.Length ?? 0}) is less than DLC({canFrame.DLC}).");

                try
                {
                    canFrame.Dir = 0;
                    var validId = CanFrameHelper.GetId(canFrame.ID);
                    var isExtended = CanFrameHelper.IsEFF(canFrame.ID);
                    var isRTR = CanFrameHelper.IsRTR(canFrame.ID);
                    //TLIBCANFD.FData 为固定 64 字节数组,需按 SizeConst 分配
                    byte[] bytes = new byte[DataLenConverter.MaxCanFdDataLength];
                    Array.Copy(canFrame.Data, 0, bytes, 0, canFrame.DLC);
                    //实际数据长度换算为 DLC 编码(0-15)
                    TLIBCANFD canfdMsg = new TLIBCANFD(_canfdConfig.ChannelIndex, (int)validId, true, isExtended, isRTR, (byte)DataLenConverter.LengthToDlc(canFrame.DLC), bytes, canFrame.FDF, canFrame.BRS);

                    _status = ToSunCanLibDLL.tscan_transmit_canfd_async(_handle, ref canfdMsg);
                    if (_status != 0) return false;
                    canFrame.Flags = ToSunCanFlagParser.Parse(canfdMsg.FProperties, canfdMsg.FFDProperties);
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
