using System.Runtime.InteropServices;
using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Intrepid, IntrepidDeviceType.Generic, CommProtocol.CAN)]
    public class IntrepidCanWrapper : IHardware
    {
        //Intrepid 设备句柄(IntPtr.Zero 表示未打开)
        private IntPtr _handle = IntPtr.Zero;
        private readonly IntrepidCANConfig _canConfig = new IntrepidCANConfig();

        // 通道号(0基) -> 设备网络 ID(HSCAN1~HSCAN7)
        private static readonly IntrepidIcsneoLibDLL.NetworkIDs[] _channelMap = new[]
        {
            IntrepidIcsneoLibDLL.NetworkIDs.HSCAN1,
            IntrepidIcsneoLibDLL.NetworkIDs.HSCAN2,
            IntrepidIcsneoLibDLL.NetworkIDs.HSCAN3,
            IntrepidIcsneoLibDLL.NetworkIDs.HSCAN4,
            IntrepidIcsneoLibDLL.NetworkIDs.HSCAN5,
            IntrepidIcsneoLibDLL.NetworkIDs.HSCAN6,
            IntrepidIcsneoLibDLL.NetworkIDs.HSCAN7,
        };

        // 接收等待超时(ms),驱动级等待不占 CPU,同时保证 Close 能在 1s 内退出读任务
        private const uint RxWaitTimeoutMs = 200;
        // 单次批量取消息的上限(参考实现同值,缓冲区在 Open 时一次性分配)
        private const int RxBufferSize = 20000;
        private IntrepidIcsneoLibDLL.SpyMessage[]? _rxMessages = null;
        private IntrepidIcsneoLibDLL.NetworkIDs _networkId = IntrepidIcsneoLibDLL.NetworkIDs.HSCAN1;

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

                IntPtr tempHandle = IntPtr.Zero;
                try
                {
                    if (_canConfig.Channel >= _channelMap.Length)
                        throw new ArgumentException($"Invalid channel({_canConfig.Channel}), valid range: 0-{_channelMap.Length - 1}(HSCAN1~HSCAN{_channelMap.Length}).");
                    _networkId = _channelMap[_canConfig.Channel];

                    // 查找第一台已连接设备并打开(bConfigRead=1,DLL 先读出设备配置)
                    tempHandle = IntrepidIcsneoLibDLL.OpenFirstDevice();

                    // 设置波特率(枚举/自定义,值均为 bps 直填,驱动自动取最近值)
                    int bitrate = _canConfig.EnableCustomBitrate ? _canConfig.CustomBitrate : (int)_canConfig.CanBitrate;
                    int result = IntrepidIcsneoLibDLL.icsneoSetBitRate(tempHandle, bitrate, _networkId);
                    if (result != 1)
                        throw new Exception($"Set bitrate {bitrate} failed({result})");

                    // 所有操作成功后，才提交状态
                    _rxMessages = new IntrepidIcsneoLibDLL.SpyMessage[RxBufferSize];
                    _handle = tempHandle;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (tempHandle != IntPtr.Zero) { try { int errors = 0; IntrepidIcsneoLibDLL.icsneoClosePort(tempHandle, ref errors); } catch { } }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _rxMessages = null;
                    _isOpen = false;
                    _handle = IntPtr.Zero;
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

                // 关闭硬件
                if (_handle != IntPtr.Zero)
                {
                    try { int errors = 0; IntrepidIcsneoLibDLL.icsneoClosePort(_handle, ref errors); } catch { }
                    _handle = IntPtr.Zero;
                }
                _rxMessages = null;
                _isOpen = false;
            }
            return true;
        }

        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                // neoVI 无专用清空函数,循环取空驱动接收队列即可
                if (_handle == IntPtr.Zero || _rxMessages == null) return;
                int numMsgs = 0, numErrors = 0;
                do
                {
                    numMsgs = 0;
                    if (IntrepidIcsneoLibDLL.icsneoGetMessages(_handle, ref _rxMessages[0], ref numMsgs, ref numErrors) != 1)
                        break;
                }
                while (numMsgs > 0);
            }
        }

        public void FlushTransmitBuffer()
        {
            // neoVI 发送直接交付设备,无用户态发送队列可清空
        }

        private void ReadTaskProc(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var rxMessages = _rxMessages;
                    var handle = _handle;
                    if (rxMessages == null || handle == IntPtr.Zero) break;

                    // 驱动级等待(最长 200ms),超时无消息则进入下一轮
                    if (IntrepidIcsneoLibDLL.icsneoWaitForRxMessagesWithTimeOut(handle, RxWaitTimeoutMs) == 0)
                        continue;

                    int numMsgs = 0, numErrors = 0;
                    if (IntrepidIcsneoLibDLL.icsneoGetMessages(handle, ref rxMessages[0], ref numMsgs, ref numErrors) != 1)
                        continue;

                    for (int i = 0; i < numMsgs; ++i)
                    {
                        var msg = rxMessages[i];
                        // 过滤:非本网络 / 发送回显(由 Send 成功后自行推送) / 非 CAN 协议消息
                        if (msg.NetworkID != (byte)_networkId
                            || (msg.StatusBitField & IntrepidIcsneoLibDLL.StatusBits.TX_MSG) != 0
                            || (msg.Protocol != IntrepidIcsneoLibDLL.Protocol.CAN && msg.Protocol != IntrepidIcsneoLibDLL.Protocol.CANFD))
                            continue;

                        // 将 EFF/RTR 标志合成到 ID 高位(SocketCAN 风格)
                        long id = msg.ArbIDOrHeader & CanFrameDef.CAN_ID_FLAG;
                        if ((msg.StatusBitField & IntrepidIcsneoLibDLL.StatusBits.XTD_FRAME) != 0) id |= CanFrameDef.CAN_EFF_FLAG;
                        if ((msg.StatusBitField & IntrepidIcsneoLibDLL.StatusBits.REMOTE_FRAME) != 0) id |= CanFrameDef.CAN_RTR_FLAG;

                        // 数据场: >8 字节走外挂指针,<=8 字节取内联 ulong(小端: byte[0] 为 LSB)
                        int length = Math.Min((int)msg.NumberBytesData, DataLenConverter.MaxCanDataLength);
                        byte[] data = new byte[length];
                        if (msg.ExtraDataPtrEnabled != 0 && msg.iExtraDataPtr != IntPtr.Zero)
                        {
                            Marshal.Copy(msg.iExtraDataPtr, data, 0, length);
                        }
                        else
                        {
                            byte[] inline = BitConverter.GetBytes(msg.Data);
                            Array.Copy(inline, 0, data, 0, length);
                        }

                        CANFrame frame1 = new CANFrame();
                        frame1.ID = id;
                        frame1.DLC = length;
                        frame1.Flag = (int)msg.StatusBitField;
                        frame1.Flags = IntrepidCanFlagParser.Parse((uint)msg.StatusBitField, (uint)msg.StatusBitField2, (uint)msg.StatusBitField3);
                        frame1.Timestamp = IntrepidIcsneoLibDLL.GetTimestampUs(msg.TimeHardware, msg.TimeHardware2);
                        frame1.Data = data;
                        frame1.Dir = 1;

                        eventHandler?.Invoke(this, frame1);
                    }
                }
                catch (Exception)
                {
                    // 其他异常，稍等后继续
                    try { if (!token.IsCancellationRequested) Thread.Sleep(10); }
                    catch { }
                }
            }
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
                    uint validId = CanFrameHelper.GetId(canFrame.ID);
                    bool isExtended = CanFrameHelper.IsEFF(canFrame.ID);
                    bool isRtr = CanFrameHelper.IsRTR(canFrame.ID);

                    // SocketCAN 高位标志 → icsSpyMessage 状态位;数据打包进内联 8 字节 ulong
                    uint statusBitField = isExtended ? (uint)IntrepidIcsneoLibDLL.StatusBits.XTD_FRAME : 0;
                    if (isRtr) statusBitField |= (uint)IntrepidIcsneoLibDLL.StatusBits.REMOTE_FRAME;

                    byte[] inline = new byte[DataLenConverter.MaxCanDataLength];
                    Array.Copy(canFrame.Data, 0, inline, 0, canFrame.DLC);
                    var msg = new IntrepidIcsneoLibDLL.SpyMessage()
                    {
                        StatusBitField = (IntrepidIcsneoLibDLL.StatusBits)statusBitField,
                        ArbIDOrHeader = validId,
                        NumberBytesData = (byte)canFrame.DLC,
                        Data = BitConverter.ToUInt64(inline, 0),
                    };

                    int result = IntrepidIcsneoLibDLL.icsneoTxMessages(_handle, ref msg, _networkId, 1);
                    if (result != 1) return false;
                    canFrame.Flag = (int)statusBitField;
                    canFrame.Flags = IntrepidCanFlagParser.Parse(statusBitField | (uint)IntrepidIcsneoLibDLL.StatusBits.TX_MSG, 0, 0);
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
