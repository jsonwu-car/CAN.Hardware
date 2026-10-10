using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    [HardwareDefAttributes(HardwareManufacturer.Kvaser, KvaserDeviceType.Generic, CommProtocol.CAN)]
    public class KvaserCanWrapper : IHardware
    {
        //kvaser handle 不支持多线程操作-->提示kvalapw2.dll 引起异常崩溃
        private int _handle = -1;
        private readonly KvaserCANConfig _canConfig = new KvaserCANConfig();
        private Canlib.canStatus _canStatus;

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
                if (_handle >= 0)
                {
                    try
                    {
                        KvaserCanLibDLL.canBusOff(_handle);
                        KvaserCanLibDLL.canClose(_handle);
                    }
                    catch { }
                    KvaserCanLibDLL.UnloadLibrary();
                    _handle = -1;
                }
                _isOpen = false;
            }
            return true;
        }
        public void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (_handle >= 0) KvaserCanLibDLL.canFlushReceiveQueue(_handle);
            }
        }
        public void FlushTransmitBuffer()
        {
            lock (_lock)
            {
                if (_handle >= 0) KvaserCanLibDLL.canFlushTransmitQueue(_handle);
            }
        }
        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;

                int tempHandle = -1;
                try
                {
                    KvaserCanLibDLL.InitializeLibrary();
                    tempHandle = KvaserCanLibDLL.canOpenChannel((int)_canConfig.CanDeviceChannel, KvaserCanLibDLL.canOPEN_ACCEPT_VIRTUAL);
                    if (tempHandle < 0) throw new Exception($"Open channel failed({tempHandle}, {((Canlib.canStatus)tempHandle).GetEnumDescription()})");

                    // 设置波特率
                    if (_canConfig.EnableCustomBitrate)
                    {
                        kvBusParamsTq dataBitra = new kvBusParamsTq(_canConfig.Tq, _canConfig.Phase1, _canConfig.Phase2, _canConfig.Sjw, _canConfig.Prop, _canConfig.Prescale);
                        _canStatus = KvaserCanLibDLL.canSetBusParamsTq(tempHandle, dataBitra);
                        if (_canStatus != Canlib.canStatus.canOK)
                            throw new Exception($"SetCustomBusParams failed({_canStatus.GetEnumDescription()})");
                    }
                    else
                    {
                        _canStatus = KvaserCanLibDLL.canSetBusParams(tempHandle, BuildCanBitrate(_canConfig.CanBitrate), 0, 0, 0, 0, 0);
                        if (_canStatus != Canlib.canStatus.canOK)
                            throw new Exception($"Set {_canConfig.CanBitrate} failed({_canStatus.GetEnumDescription()})");
                    }

                    // 启动总线
                    _canStatus = KvaserCanLibDLL.canBusOn(tempHandle);
                    if (_canStatus != Canlib.canStatus.canOK)
                        throw new Exception($"Open bus on failed({_canStatus.GetEnumDescription()})");

                    // 设置过滤器
                    if (_canConfig.EnableFilter)
                    {
                        _canStatus = KvaserCanLibDLL.canSetAcceptanceFilter(tempHandle, _canConfig.Filter_Code, _canConfig.Filter_Mask, (_canConfig.Fileter_IsExtended ? 1 : 0));
                        if (_canStatus != Canlib.canStatus.canOK)
                            throw new Exception($"Set frame filter failed({_canStatus.GetEnumDescription()})");
                    }
                    else
                    {
                        _canStatus = KvaserCanLibDLL.canSetAcceptanceFilter(tempHandle, 0, 0, 0);
                        if (_canStatus != Canlib.canStatus.canOK)
                            throw new Exception($"Initial can filter failed({_canStatus.GetEnumDescription()})");
                    }

                    //设置时间戳最小单位为1微妙
                    _canStatus = KvaserCanLibDLL.SetTimeScale(tempHandle, 1, 4);
                    if (_canStatus != Canlib.canStatus.canOK)
                        throw new Exception($"set can scale failed({_canStatus.GetEnumDescription()})");

                    // 所有操作成功后，才提交状态
                    _handle = tempHandle;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 清理已打开的资源
                    if (tempHandle >= 0) { try { KvaserCanLibDLL.canBusOff(tempHandle); KvaserCanLibDLL.canClose(tempHandle); } catch { } }
                    if (_cts != null) { _cts.Dispose(); _cts = null; }
                    _isOpen = false;
                    _handle = -1;
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
                    bool res = false;
                    // 连续读取直到无数据
                    do
                    {
                        if (token.IsCancellationRequested) break;
                        CANFrame? frame = null;
                        res = Read(ref frame);
                    }
                    while (res && !token.IsCancellationRequested);

                    // 使用Task.Delay并正确处理取消
                    if (!token.IsCancellationRequested) await Task.Delay(5, token);
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，退出循环
                    break;
                }
                catch (Exception)
                {
                    // 其他异常，稍等后继续
                    try { if (!token.IsCancellationRequested) await Task.Delay(10, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }
        private unsafe bool Read(ref CANFrame? frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _handle < 0) { frame = null; return false; }

                try
                {
                    CANFrame frame1 = new CANFrame();
                    byte* ptr = stackalloc byte[64];
                    int id2;
                    uint dlc2, flag2, time2;
                    _canStatus = KvaserCanLibDLL.canRead(_handle, out id2, (IntPtr)ptr, out dlc2, out flag2, out time2);
                    //钳制dlc到经典CAN有效范围，防止驱动返回异常值导致越界
                    dlc2 = Math.Min(dlc2, DataLenConverter.MaxCanDataLength);
                    frame1.ID = CanFrameHelper.GetId(id2);
                    frame1.DLC = (int)dlc2;
                    frame1.Flag = (int)flag2;
                    frame1.Flags = KvaserCanFlagParser.Parse(flag2);
                    frame1.Timestamp = time2;
                    frame1.Data = new byte[dlc2];
                    frame1.Dir = 1;
                    frame = frame1;

                    if (_canStatus == Canlib.canStatus.canOK && id2 != 0)
                    {
                        long sourceBytesToCopy = Math.Min(Math.Min(frame.Data.Length, 64), frame1.DLC);
                        fixed (byte* destination = frame.Data)
                        {
                            Buffer.MemoryCopy(ptr, destination, frame.Data.Length, sourceBytesToCopy);
                        }
                        eventHandler?.Invoke(this, frame);
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Read)}:{ex.Message}");
                }
            }
        }
        public unsafe bool Send(FrameBase frame)
        {
            lock (_lock)
            {
                if (!_isOpen || _handle < 0) return false;

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
                    int maxWriteCount = 0;
                    canFrame.Dir = 0;
                    var validId = CanFrameHelper.GetId(canFrame.ID);
                    var isExtended = CanFrameHelper.IsEFF(canFrame.ID);
                    var isRTR = CanFrameHelper.IsRTR(canFrame.ID);
                    var flag = isExtended ? KvaserCanLibDLL.canMSG_EXT : KvaserCanLibDLL.canMSG_STD;
                    flag |= isRTR ? KvaserCanLibDLL.canMSG_RTR : 0;

                    fixed (byte* ptr = canFrame.Data)
                    {
                        _canStatus = KvaserCanLibDLL.canWrite(_handle, (int)validId, (IntPtr)ptr, (uint)canFrame.DLC, (uint)flag);
                    }
                    //Kvaser CAN 驱动bug，临时方案：写入失败再次写入，超过5次报错；
                    while (_canStatus == Canlib.canStatus.canERR_TXBUFOFL && maxWriteCount < 5)
                    {
                        fixed (byte* ptr = canFrame.Data)
                        {
                            _canStatus = KvaserCanLibDLL.canWrite(_handle, (int)validId, (IntPtr)ptr, (uint)canFrame.DLC, (uint)flag);
                        }
                        if (_canStatus == Canlib.canStatus.canOK) break;
                        Thread.Sleep(50);
                        maxWriteCount++;
                    }
                    if (_canStatus != Canlib.canStatus.canOK) return false;
                    canFrame.Flags = KvaserCanFlagParser.Parse((uint)flag);
                    eventHandler?.Invoke(this, canFrame);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(Send)}:{ex.Message}");
                }
            }
        }
        private int BuildCanBitrate(KvaserCanBitrate canBitRate)
        {
            switch (canBitRate)
            {
                case KvaserCanBitrate.canBITRATE_10K: return KvaserCanLibDLL.canBITRATE_10K;
                case KvaserCanBitrate.canBITRATE_50K: return KvaserCanLibDLL.canBITRATE_50K;
                case KvaserCanBitrate.canBITRATE_62K: return KvaserCanLibDLL.canBITRATE_62K;
                case KvaserCanBitrate.canBITRATE_83K: return KvaserCanLibDLL.canBITRATE_83K;
                case KvaserCanBitrate.canBITRATE_100K: return KvaserCanLibDLL.canBITRATE_100K;
                case KvaserCanBitrate.canBITRATE_125K: return KvaserCanLibDLL.canBITRATE_125K;
                case KvaserCanBitrate.canBITRATE_250K: return KvaserCanLibDLL.canBITRATE_250K;
                case KvaserCanBitrate.canBITRATE_500K: return KvaserCanLibDLL.canBITRATE_500K;
                case KvaserCanBitrate.canBITRATE_1M: return KvaserCanLibDLL.canBITRATE_1M;
                default: return KvaserCanLibDLL.canBITRATE_500K;
            }
        }
    }
}
