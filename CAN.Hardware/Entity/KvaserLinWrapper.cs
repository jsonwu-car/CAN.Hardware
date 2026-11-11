using CAN.Hardware.Interface;
using CAN.Hardware.Attributes;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Parameters;
using CAN.Hardware.Comm;
using CAN.Hardware.Model;

namespace CAN.Hardware.Entity
{
    /// <summary>
    /// Kvaser LIN(linlib.dll)灏佽銆?
    /// 涓夌鑺傜偣妯″紡鐨勮涔?
    /// - Master:Send 鍙戦€佸畬鏁村抚(鎶ュご+鍝嶅簲),RequestMessage 鎸夋寚瀹?ID 鍙戦€佹姤澶?搴旂瓟鐢卞悗鍙颁换鍔＄粡浜嬩欢涓婃姏;
    /// - Slave:Send 鏇存柊鏈妭鐐瑰簲绛旀暟鎹?linUpdateMessage),鏀跺埌鍖归厤鎶ュご鏃剁敱椹卞姩鍙戝嚭;
    /// - Monitor:浠?slave 鎵撳紑浣嗘案涓嶆洿鏂板簲绛旀暟鎹?鍙敹涓嶅彂銆?
    /// </summary>
    [HardwareDefAttributes(HardwareManufacturer.Kvaser, KvaserDeviceType.Generic, CommProtocol.LIN)]
    public class KvaserLinWrapper : ILinHardware
    {
        private const int LinMaxDlc = 8;
        private const long LinMaxId = 0x3F;

        private int _handle = -1;
        private KvaserLinConfig _kvaserLinConfig = new KvaserLinConfig();
        private Linlib.LinStatus _linStatus;

        // 鐢ㄤ簬瀵瑰鎺ㄩ€佹姤鏂?
        public EventHandler<FrameBase>? eventHandler { get; set; } = null;

        // 鐢ㄤ簬纭欢鐨勯厤缃?鐢ㄤ簬纭欢鐨勫垵濮嬪寲锛屼害鍙敤浜嶹PF Xaml鐨凚inding)
        public BusConfigBase Config => _kvaserLinConfig;

        // 浣跨敤鍗曚竴閿佷繚鎶ゆ墍鏈夌姸鎬佸拰纭欢鎿嶄綔
        private readonly object _lock = new object();

        // Task鎺у埗鐩稿叧
        private bool _isOpen = false;
        private bool _closing = false;
        private Task? _readTask = null;
        private CancellationTokenSource? _cts = null;

        public object GetConfig() { return _kvaserLinConfig; }

        public bool Close()
        {
            Task? taskToWait;
            CancellationTokenSource? ctsToCancel;
            int handleToClose;

            lock (_lock)
            {
                if (!_isOpen) return true;
                _isOpen = false;
                _closing = true;
                handleToClose = _handle;
                ctsToCancel = _cts;
                taskToWait = _readTask;
                _cts = null;
                _readTask = null;
                if (ctsToCancel != null) ctsToCancel.Cancel();
            }

            // 鍦ㄩ攣澶栭儴绛夊緟Task瀹屾垚,閬垮厤姝婚攣
            if (taskToWait != null)
            {
                try { taskToWait.Wait(1000); }
                catch (AggregateException) { }
                catch (Exception) { }
            }

            lock (_lock)
            {
                try
                {
                    if (handleToClose >= 0)
                    {
                        KvaserLinLibDLL.linBusOff(handleToClose);
                        KvaserLinLibDLL.linClose(handleToClose);
                    }
                }
                catch { }
                if (_handle == handleToClose) _handle = -1;
                KvaserLinLibDLL.UnloadLibrary();
                if (ctsToCancel != null)
                {
                    try { ctsToCancel.Dispose(); }
                    catch (Exception) { }
                }
                _closing = false;
            }
            return true;
        }

        public unsafe void FlushReceiveBuffer()
        {
            lock (_lock)
            {
                if (!_isOpen || _handle < 0) return;
                try
                {
                    // linlib 鏃犱笓鐢ㄦ竻闃熷垪鎺ュ彛,閫愬抚璇诲嚭鐩磋嚦 linERR_NOMSG
                    byte* ptr = stackalloc byte[LinMaxDlc];
                    while (true)
                    {
                        uint id, dlc, flags;
                        KvaserLinLibDLL.LinMessageInfo msgInfo = new KvaserLinLibDLL.LinMessageInfo();
                        _linStatus = KvaserLinLibDLL.linReadMessage(_handle, out id, (IntPtr)ptr, out dlc, out flags, ref msgInfo);
                        if (_linStatus != Linlib.LinStatus.linOK) break;
                    }
                }
                catch { }
            }
        }

        public void FlushTransmitBuffer()
        {
            // linlib 鍙戦€佷负绔嬪嵆浜や粯,鏃犲彲娓呯殑鍙戦€佺紦鍐插尯,淇濇寔绌哄疄鐜?
        }

        public bool Open()
        {
            lock (_lock)
            {
                if (_isOpen) return true;
                if (_closing) return false;
                if (_kvaserLinConfig == null) return false;

                int tempHandle = -1;
                bool libraryLoaded = false;
                bool busOn = false;
                try
                {
                    KvaserLinLibDLL.InitializeLibrary();
                    libraryLoaded = true;

                    tempHandle = KvaserLinLibDLL.linOpenChannel((int)_kvaserLinConfig.DeviceChannel, MapOpenFlag(_kvaserLinConfig.NodeMode));
                    if (tempHandle < 0) throw new Exception($"open channel failed({((Linlib.LinStatus)tempHandle).GetEnumDescription()})");

                    _linStatus = KvaserLinLibDLL.linSetBitrate(tempHandle, _kvaserLinConfig.LinBitrate);
                    if (_linStatus != Linlib.LinStatus.linOK) throw new Exception($"Set lin bitrate failed({_linStatus.GetEnumDescription()})");

                    _linStatus = KvaserLinLibDLL.linBusOn(tempHandle);
                    if (_linStatus != Linlib.LinStatus.linOK) throw new Exception($"Bus on failed({_linStatus.GetEnumDescription()})");
                    busOn = true;

                    _linStatus = KvaserLinLibDLL.linSetupLIN(tempHandle, (uint)((_kvaserLinConfig.ChecksumType ? Linlib.LIN_ENHANCED_CHECKSUM : 0) | (_kvaserLinConfig.Variable_DLC ? Linlib.LIN_VARIABLE_DLC : 0)), _kvaserLinConfig.LinBitrate);
                    if (_linStatus != Linlib.LinStatus.linOK) throw new Exception($"Setup lin failed({_linStatus.GetEnumDescription()})");

                    // 鎵€鏈夋搷浣滄垚鍔熷悗锛屾墠鎻愪氦鐘舵€?
                    _handle = tempHandle;
                    _cts = new CancellationTokenSource();
                    _isOpen = true;
                    _readTask = Task.Run(() => ReadTaskProc(_cts.Token), _cts.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    // 娓呯悊宸叉墦寮€鐨勮祫婧?
                    if (tempHandle >= 0)
                    {
                        if (busOn) { try { KvaserLinLibDLL.linBusOff(tempHandle); } catch { } }
                        try { KvaserLinLibDLL.linClose(tempHandle); } catch { }
                    }
                    if (libraryLoaded) KvaserLinLibDLL.UnloadLibrary();
                    if (_cts != null) { try { _cts.Dispose(); } catch { } _cts = null; }
                    _readTask = null;
                    _isOpen = false;
                    _handle = -1;
                    throw new Exception($"{nameof(Open)}:{ex.Message}");
                }
            }
        }

        private static int MapOpenFlag(LinNodeMode nodeMode)
        {
            switch (nodeMode)
            {
                case LinNodeMode.Master: return Linlib.LIN_MASTER;
                case LinNodeMode.Slave: return Linlib.LIN_SLAVE;
                // linlib 鏃犵嫭绔嬬洃鍚ā寮?鏍囧噯鍋氭硶鏄互 slave 鎵撳紑銆佹案涓嶈皟鐢?linUpdateMessage,
                // 鍗冲彧鏀朵笉鍙?WriteLocked 涓凡闃绘 Monitor 涓诲姩鍙戦€?
                case LinNodeMode.Monitor: return Linlib.LIN_SLAVE;
                default: throw new Exception($"Unsupported node type({nodeMode})");
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
            if (linId < 0 || linId > LinMaxId) return false;
            lock (_lock)
            {
                if (!_isOpen || _handle < 0) return false;
                if (_kvaserLinConfig.NodeMode != LinNodeMode.Master) return false;
                try
                {
                    _linStatus = KvaserLinLibDLL.linRequestMessage(_handle, (uint)linId);
                    return _linStatus == Linlib.LinStatus.linOK;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(RequestMessage)}:{ex.Message}");
                }
            }
        }

        public bool SetChecksumType(bool enhanced)
        {
            lock (_lock)
            {
                _kvaserLinConfig.ChecksumType = enhanced;
                if (!_isOpen || _handle < 0) return true;
                try
                {
                    _linStatus = KvaserLinLibDLL.linSetupLIN(_handle, (uint)((enhanced ? Linlib.LIN_ENHANCED_CHECKSUM : 0) | (_kvaserLinConfig.Variable_DLC ? Linlib.LIN_VARIABLE_DLC : 0)), _kvaserLinConfig.LinBitrate);
                    return _linStatus == Linlib.LinStatus.linOK;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(SetChecksumType)}:{ex.Message}");
                }
            }
        }

        public bool SetIllegalMessage(long linId, bool checksumError, bool parityError, uint delayBitTimes)
        {
            if (linId < 0 || linId > LinMaxId)
                throw new Exception($"{nameof(SetIllegalMessage)}:Invalid frame id({linId})");
            lock (_lock)
            {
                if (!_isOpen || _handle < 0)
                    throw new Exception($"{nameof(SetIllegalMessage)}:LIN channel is not open");
                try
                {
                    uint cFlags = 0;
                    if (checksumError) cFlags |= (uint)Linlib.LIN_MSG_DISTURB_CSUM;
                    if (parityError) cFlags |= (uint)Linlib.LIN_MSG_DISTURB_PARITY;
                    _linStatus = KvaserLinLibDLL.linSetupIllegalMessage(_handle, (uint)linId, cFlags, delayBitTimes);
                    if (_linStatus != Linlib.LinStatus.linOK)
                        throw new Exception($"Set illegal message failed({_linStatus.GetEnumDescription()})");
                    return true;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{nameof(SetIllegalMessage)}:{ex.Message}");
                }
            }
        }

        public bool ClearMessage(long linId)
        {
            if (linId < 0 || linId > LinMaxId)
                throw new Exception($"{nameof(ClearMessage)}:Invalid frame id({linId})");
            lock (_lock)
            {
                if (!_isOpen || _handle < 0)
                    throw new Exception($"{nameof(ClearMessage)}:LIN channel is not open");
                if (_kvaserLinConfig.NodeMode != LinNodeMode.Slave)
                    throw new Exception($"{nameof(ClearMessage)}:Not in slave mode({_kvaserLinConfig.NodeMode})");
                try
                {
                    _linStatus = KvaserLinLibDLL.linClearMessage(_handle, (uint)linId);
                    if (_linStatus != Linlib.LinStatus.linOK)
                        throw new Exception($"Clear message failed({_linStatus.GetEnumDescription()})");
                    // linlib 鍛戒护鏄帓闃熶笅鍙戠殑,linOK 浠呬唬琛ㄩ┍鍔ㄥ凡鎺ュ彈;
                    // 瀹樻柟寤鸿鍦?linClearMessage 鍚庤皟鐢?linWriteSync,纭繚娓呴櫎鍦?LIN 鎺ュ彛渚х敓鏁?
                    KvaserLinLibDLL.linWriteSync(_handle, 100);
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
                if (!_isOpen || _handle < 0)
                    throw new Exception($"{nameof(WakeUp)}:LIN channel is not open");
                if (_kvaserLinConfig.NodeMode != LinNodeMode.Master)
                    throw new Exception($"{nameof(WakeUp)}:Not in master mode({_kvaserLinConfig.NodeMode})");
                try
                {
                    _linStatus = KvaserLinLibDLL.linWriteWakeup(_handle, 1, 0);
                    if (_linStatus != Linlib.LinStatus.linOK)
                        throw new Exception($"Wake up failed({_linStatus.GetEnumDescription()})");
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
                if (!_isOpen || _handle < 0)
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

        private unsafe bool WriteLocked(LINFrame linFrame)
        {
            if (_kvaserLinConfig.NodeMode == LinNodeMode.Master)
            {
                // 涓昏妭鐐?鍙戦€佸畬鏁村抚(鎶ュご+鍝嶅簲),鎶ユ枃鍗冲埢涓婄嚎
                fixed (byte* ptr = linFrame.Data)
                {
                    _linStatus = KvaserLinLibDLL.linWriteMessage(_handle, (uint)linFrame.ID, (IntPtr)ptr, (uint)linFrame.Data.Length);
                }
            }
            else if (_kvaserLinConfig.NodeMode == LinNodeMode.Slave)
            {
                // 浠庤妭鐐?鏇存柊鏈妭鐐硅 ID 鐨勫簲绛旀暟鎹?寰呬富鑺傜偣鎶ュご鍒拌揪鏃剁敱椹卞姩鍙戝嚭
                fixed (byte* ptr = linFrame.Data)
                {
                    _linStatus = KvaserLinLibDLL.linUpdateMessage(_handle, (uint)linFrame.ID, (IntPtr)ptr, (uint)linFrame.Data.Length);
                }
            }
            else
            {
                // 鐩戝惉妯″紡鍙敹涓嶅彂
                return false;
            }
            if (_linStatus != Linlib.LinStatus.linOK) return false;
            return true;
        }

        private unsafe bool TryReadFrameLocked(out LINFrame? frame)
        {
            frame = null;
            if (!_isOpen || _handle < 0) return false;

            byte* ptr = stackalloc byte[LinMaxDlc];
            while (true)
            {
                uint id, dlc, flags;
                KvaserLinLibDLL.LinMessageInfo msgInfo = new KvaserLinLibDLL.LinMessageInfo();
                _linStatus = KvaserLinLibDLL.linReadMessage(_handle, out id, (IntPtr)ptr, out dlc, out flags, ref msgInfo);
                if (_linStatus != Linlib.LinStatus.linOK) return false;
                if (dlc > LinMaxDlc) continue;
                frame = BuildFrame(id, dlc, flags, ptr, msgInfo);
                return true;
            }
        }

        private unsafe LINFrame BuildFrame(uint id, uint dlc, uint flags, byte* data, KvaserLinLibDLL.LinMessageInfo msgInfo)
        {
            LINFrame frame = new LINFrame();
            frame.ID = id;
            frame.DLC = (int)dlc;
            frame.Data = new byte[dlc];
            for (int i = 0; i < dlc; i++) frame.Data[i] = data[i];
            frame.Flag = (int)flags;
            frame.Flags = KvaserLinFlagParser.Parse(flags, msgInfo);
            frame.Timestamp = msgInfo.timestamp;
            frame.Dir = (flags & (uint)Linlib.LIN_TX) != 0 ? 0 : 1;
            return frame;
        }
    }
}
