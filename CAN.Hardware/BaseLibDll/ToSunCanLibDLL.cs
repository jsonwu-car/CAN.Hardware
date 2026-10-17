using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
namespace CAN.Hardware.BaseLibDll
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void TCANQueueEvent_Win32(ref TLIBCAN AData);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void TCANFDQueueEvent_Win32(ref TLIBCANFD AData);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void TLINQueueEvent_Win32(ref TLIBLIN AData);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void TTSCANConnectedCallback_Win32(IntPtr ADevicehandle);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void N_USData_TranslateCompleted_Recall(int ATpModuleIndex, int AChn, ulong ATimeStamp, IntPtr APayLoad, uint ASize, CAN_ISO_TP_RESAULT AError);
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TLIBCAN
    {
        public byte FIdxChn;

        public byte FProperties;

        public byte FDLC;

        public byte FReserved;

        public int FIdentifier;

        public ulong FTimeUS;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] FData;

        public bool FIsTx
        {
            get
            {
                return (FProperties & 1) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 1;
                    return;
                }

                byte b = 1;
                FProperties &= (byte)(~b);
            }
        }

        public bool FIsLogged
        {
            get
            {
                return (FProperties & 0x40) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 64;
                    return;
                }

                byte b = 64;
                FProperties &= (byte)(~b);
            }
        }

        public bool FIsExt
        {
            get
            {
                return (FProperties & 4) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 4;
                    return;
                }

                byte b = 4;
                FProperties &= (byte)(~b);
            }
        }

        public bool FIsRemote
        {
            get
            {
                return (FProperties & 2) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 2;
                    return;
                }

                byte b = 2;
                FProperties &= (byte)(~b);
            }
        }

        public bool FIsError
        {
            get
            {
                return (FProperties & 0x80) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 128;
                    return;
                }

                byte b = 128;
                FProperties &= (byte)(~b);
            }
        }

        public string GetString()
        {
            string text = "Classic CAN:" + FTimeUS + ":";
            text = ((!FIsTx) ? (text + "Rx: ") : (text + "Tx: "));
            text = text + FIdentifier.ToString("X8") + " ";
            text = text + FDLC + " ";
            for (int i = 0; i < FDLC; i++)
            {
                text += FData[i].ToString("X2");
            }

            return text;
        }

        public TLIBCAN(CHANNEL_INDEX AIdxChn, int AID, bool AIsTx, bool AIsExt, bool AIsRemote, byte ADLC, byte[] ADataArray)
        {
            FIdxChn = (byte)AIdxChn;
            FProperties = 0;
            FIdentifier = AID;
            FDLC = ADLC;
            FTimeUS = 0uL;
            FData = new byte[8];
            int num = ADataArray.Count();
            if (num > 8)
            {
                num = 8;
            }

            for (int i = 0; i < num; i++)
            {
                FData[i] = ADataArray[i];
            }

            FReserved = 0;
            FIsTx = AIsTx;
            FIsError = false;
            FIsExt = AIsExt;
            FIsRemote = AIsRemote;
        }

        public TLIBCAN(CHANNEL_INDEX AIdxChn, int AID, bool AIsTx, bool AIsExt, bool AIsRemote, byte ADLC)
        {
            FIdxChn = (byte)AIdxChn;
            FIdentifier = AID;
            FProperties = 0;
            FDLC = ADLC;
            FTimeUS = 0uL;
            FData = new byte[8];
            FReserved = 0;
            FIsTx = AIsTx;
            FIsError = false;
            FIsExt = AIsExt;
            FIsRemote = AIsRemote;
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TLIBCANFD
    {
        public byte FIdxChn;

        public byte FProperties;

        public byte FDLC;

        public byte FFDProperties;

        public int FIdentifier;

        public ulong FTimeUS;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] FData;

        public static readonly byte[] DLC_DATA_BYTE_CNT = new byte[16]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 12,
            16, 20, 24, 32, 48, 64
        };

        public bool FIsTx
        {
            get
            {
                return (FProperties & 1) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 1;
                    return;
                }

                byte b = 1;
                FProperties &= (byte)(~b);
            }
        }

        public bool FIsLogged
        {
            get
            {
                return (FProperties & 0x40) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 64;
                    return;
                }

                byte b = 64;
                FProperties &= (byte)(~b);
            }
        }

        public bool FIsExt
        {
            get
            {
                return (FProperties & 4) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 4;
                    return;
                }

                byte b = 4;
                FProperties &= (byte)(~b);
            }
        }

        public bool FIsRemote
        {
            get
            {
                return (FProperties & 2) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 2;
                    return;
                }

                byte b = 2;
                FProperties &= (byte)(~b);
            }
        }

        public bool FIsError
        {
            get
            {
                return (FProperties & 0x80) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 128;
                    return;
                }

                byte b = 128;
                FProperties &= (byte)(~b);
            }
        }

        public bool FIsFD
        {
            get
            {
                return (FFDProperties & 1) != 0;
            }
            set
            {
                if (value)
                {
                    FFDProperties |= 1;
                    return;
                }

                byte b = 1;
                FFDProperties &= (byte)(~b);
            }
        }

        public bool FIsBRS
        {
            get
            {
                return (FFDProperties & 2) != 0;
            }
            set
            {
                if (value)
                {
                    FFDProperties |= 2;
                    return;
                }

                byte b = 2;
                FFDProperties &= (byte)(~b);
            }
        }

        public bool FIsESI
        {
            get
            {
                return (FFDProperties & 4) != 0;
            }
            set
            {
                if (value)
                {
                    FFDProperties |= 4;
                    return;
                }

                byte b = 4;
                FFDProperties &= (byte)(~b);
            }
        }

        public string GetString()
        {
            string text = "";
            text = ((!FIsFD) ? ("Classic CAN:" + FTimeUS + ":") : ("FD CAN:" + FTimeUS + ":"));
            text = ((!FIsTx) ? (text + "Rx: ") : (text + "Tx: "));
            text = text + FIdentifier.ToString("X8") + " ";
            text = text + FDLC + " ";
            FDLC &= 15;
            byte[] array = new byte[16]
            {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 12,
                16, 20, 24, 32, 48, 64
            };
            FDLC = array[FDLC];
            for (int i = 0; i < FDLC; i++)
            {
                text += FData[i].ToString("X2");
            }

            return text;
        }

        public void SetStdId(int AId, byte ADLC)
        {
            FIdentifier = AId;
            FDLC = ADLC;
            FProperties &= 251;
        }

        public void SetExtId(int AId, byte ADLC)
        {
            FIdentifier = AId;
            FDLC = ADLC;
            FProperties |= 4;
        }

        public TLIBCANFD(CHANNEL_INDEX AIdxChn, int AID, bool AIsTx, bool AIsExt, bool AIsRemote, byte ADLC, byte[] ADataArray)
        {
            FIdxChn = (byte)AIdxChn;
            FProperties = 0;
            FIdentifier = AID;
            FDLC = ADLC;
            FTimeUS = 0uL;
            FData = new byte[64];
            int num = ADataArray.Count();
            if (num > 64)
            {
                num = 64;
            }

            for (int i = 0; i < num; i++)
            {
                FData[i] = ADataArray[i];
            }

            FFDProperties = 0;
            FIsTx = AIsTx;
            FIsError = false;
            FIsExt = AIsExt;
            FIsRemote = AIsRemote;
        }

        public TLIBCANFD(CHANNEL_INDEX AIdxChn, int AID, bool AIsTx, bool AIsExt, bool AIsRemote, byte ADLC, bool AIsFD = false, bool AIsBRS = false)
        {
            FIdxChn = (byte)AIdxChn;
            FIdentifier = AID;
            FProperties = 0;
            FDLC = ADLC;
            FTimeUS = 0uL;
            FData = new byte[64];
            FFDProperties = 0;
            FIsTx = AIsTx;
            FIsError = false;
            FIsExt = AIsExt;
            FIsRemote = AIsRemote;
            FIsFD = AIsFD;
            FIsBRS = AIsBRS;
        }

        public TLIBCANFD(CHANNEL_INDEX AIdxChn, int AID, bool AIsTx, bool AIsExt, bool AIsRemote, byte ADLC, byte[] ADatas, bool AIsFD = false, bool AIsBRS = false)
        {
            FIdxChn = (byte)AIdxChn;
            FIdentifier = AID;
            FProperties = 0;
            FDLC = ADLC;
            FTimeUS = 0uL;
            FData = ADatas;
            FFDProperties = 0;
            FIsTx = AIsTx;
            FIsError = false;
            FIsExt = AIsExt;
            FIsRemote = AIsRemote;
            FIsFD = AIsFD;
            FIsBRS = AIsBRS;
        }

        private int GetDataLength()
        {
            if (FIsFD)
            {
                if (FDLC >= 16)
                {
                    FDLC = 15;
                }

                return DLC_DATA_BYTE_CNT[FDLC];
            }

            return FDLC;
        }
    }
    public enum CHANNEL_INDEX : byte
    {
        CHN1,
        CHN2,
        CHN3,
        CHN4,
        CHN5,
        CHN6,
        CHN7,
        CHN8,
        CHN9,
        CHN10,
        CHN11,
        CHN12,
        CHN13,
        CHN14,
        CHN15,
        CHN16,
        CHN17,
        CHN18,
        CHN19,
        CHN20,
        CHN21,
        CHN22,
        CHN23,
        CHN24,
        CHN25,
        CHN26,
        CHN27,
        CHN28,
        CHN29,
        CHN30,
        CHN31,
        CHN32
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TLIBLIN
    {
        private enum PropertyLINMask
        {
            MASK_IS_TX = 1,
            MASK_IS_SENDBREAK = 2,
            MASK_IS_RECEIVEDBREAK = 4,
            MASK_IS_RECEIVEDSYNC = 8,
            MASK_HW_TYPE = 48,
            MASK_IS_LOGGED = 0x40,
            MASK_RESERVED = 0x80
        }

        public byte FIdxChn;

        public byte FErrStatus;

        public byte FProperties;

        public byte FDLC;

        public byte FIdentifier;

        public byte FChecksum;

        public byte FStatus;

        public ulong FTimeUS;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] FData;

        public bool FIsTx
        {
            get
            {
                return (FProperties & 1) != 0;
            }
            set
            {
                if (value)
                {
                    FProperties |= 1;
                    return;
                }

                byte b = 1;
                FProperties &= (byte)(~b);
            }
        }

        public TLIBLIN(CHANNEL_INDEX AIdxChn, byte AID, byte ADLC, bool AIsTx, byte[] ADataArray)
        {
            FIdxChn = (byte)AIdxChn;
            FProperties = 0;
            FIdentifier = AID;
            FDLC = ADLC;
            FTimeUS = 0uL;
            FData = new byte[8];
            int num = ADataArray.Count();
            if (num > 8)
            {
                num = 8;
            }

            for (int i = 0; i < num; i++)
            {
                FData[i] = ADataArray[i];
            }

            FChecksum = 0;
            FStatus = 0;
            FErrStatus = 0;
            FIsTx = AIsTx;
        }

        public TLIBLIN(CHANNEL_INDEX AIdxChn, byte AID, byte ADLC, bool AIsTx)
        {
            FIdxChn = (byte)AIdxChn;
            FIdentifier = AID;
            FProperties = 0;
            FDLC = ADLC;
            FTimeUS = 0uL;
            FData = new byte[8];
            FChecksum = 0;
            FStatus = 0;
            FErrStatus = 0;
            FIsTx = AIsTx;
        }
    }
    public enum READ_TX_RX_DEF : byte
    {
        ONLY_RX_MESSAGES,
        TX_RX_MESSAGES
    }
    public enum CAN_ISO_TP_RESAULT
    {
        N_OK = 0,
        IDX_ERR_TP_TIMEOUT_AS = 139,
        IDX_ERR_TP_TIMEOUT_AR = 140,
        IDX_ERR_TP_TIMEOUT_BS = 141,
        IDX_ERR_TP_TIMEOUT_CR = 142,
        IDX_ERR_TP_WRONG_SN = 143,
        IDX_ERR_TP_INVALID_FS = 144,
        IDX_ERR_TP_UNEXP_PDU = 145,
        IDX_ERR_TP_WFT_OVRN = 146,
        IDX_ERR_TP_BUFFER_OVFLW = 147,
        IDX_ERR_TP_NOT_IDLE = 148,
        IDX_ERR_TP_ERROR_FROM_CAN_DRIVER = 149
    }
    public enum TSTATISTICTYPE
    {
        IDX_CAN_STAT_BUSLOAD,
        IDX_CAN_STAT_PEAKLOAD,
        IDX_CAN_STAT_STD_DATA_RATE,
        IDX_CAN_STAT_STD_DATA_ALL,
        IDX_CAN_STAT_EXT_DATA_RATE,
        IDX_CAN_STAT_EXT_DATA_ALL,
        IDX_CAN_STAT_STD_REMOTE_RATE,
        IDX_CAN_STAT_STD_REMOTE_ALL,
        IDX_CAN_STAT_EXT_REMOTE_RATE,
        IDX_CAN_STAT_EXT_REMOTE_ALL,
        IDX_CAN_STAT_ERR_FRAME_RATE,
        IDX_CAN_STAT_ERR_FRAME_ALL
    }
    public enum TLIBCANFDControllerType
    {
        lfdtCAN,
        lfdtISOCANFD,
        lfdtNonISOCANFD
    }
    public enum TLIBCANFDControllerMode
    {
        lfdmNormal,
        lfdmACKOff,
        lfdmRestricted
    }
    public enum LIN_PROTOCOL
    {
        LIN_PROTOCOL_13,
        LIN_PROTOCOL_20,
        LIN_PROTOCOL_21,
        LIN_PROTOCOL_J2602
    }
    public enum T_LIN_NODE_FUNCTION : byte
    {
        T_MASTER_NODE,
        T_SLAVE_NODE,
        T_MONITOR_NODE
    }
    internal class ToSunCanLibDLL
    {
        private const string TSCANDLL_NAME = ".\\HwDependency\\Tosun\\Generic\\Win\\x64\\libTSCAN.dll";

        private static int _refCount = 0;
        private static readonly object _initLock = new object();

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void initialize_lib_tscan(bool AEnableFIFO, bool AEnableErrorFrame, bool AEnableTurbe);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void finalize_lib_tscan();

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_scan_devices(ref uint ADeviceCount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_get_device_info(uint ADeviceIndex, ref IntPtr AFManufacturer, ref IntPtr AFProduct, ref IntPtr AFSerial);

        public static void InitializeLibrary(bool AEnableFIFO, bool AEnableErrorFrame, bool AEnableTurbe)
        {
            lock (_initLock)
            {
                if (_refCount == 0)
                {
                    initialize_lib_tscan(AEnableFIFO, AEnableErrorFrame, AEnableTurbe);
                }
                _refCount++;
            }
        }

        public static void FinalizeLibrary()
        {
            lock (_initLock)
            {
                if (_refCount > 0)
                {
                    _refCount--;
                    if (_refCount == 0)
                    {
                        finalize_lib_tscan();
                    }
                }
            }
        }

        // 共享设备句柄管理：同一物理设备只需连接一次，多实例/多通道复用
        private static IntPtr _sharedDeviceHandle = IntPtr.Zero;
        private static int _deviceRefCount = 0;
        private static readonly object _deviceLock = new object();

        public static uint ConnectDevice(string serial, bool AEnableFIFO, bool AEnableErrorFrame, bool AEnableTurbe, out IntPtr handle)
        {
            lock (_deviceLock)
            {
                // 库级初始化（仅在首次时真正执行）
                lock (_initLock)
                {
                    if (_refCount == 0)
                    {
                        initialize_lib_tscan(AEnableFIFO, AEnableErrorFrame, AEnableTurbe);
                    }
                    _refCount++;
                }

                // 设备级连接（仅在首次时真正执行）
                if (_sharedDeviceHandle != IntPtr.Zero)
                {
                    handle = _sharedDeviceHandle;
                    _deviceRefCount++;
                    return 0;
                }

                uint status = tscan_connect(serial, ref _sharedDeviceHandle);
                if (status == 0)
                {
                    handle = _sharedDeviceHandle;
                    _deviceRefCount = 1;
                }
                else
                {
                    handle = IntPtr.Zero;
                }
                return status;
            }
        }

        public static void DisconnectDevice()
        {
            lock (_deviceLock)
            {
                if (_deviceRefCount > 0)
                {
                    _deviceRefCount--;
                    if (_deviceRefCount == 0 && _sharedDeviceHandle != IntPtr.Zero)
                    {
                        tscan_disconnect_by_handle(_sharedDeviceHandle);
                        _sharedDeviceHandle = IntPtr.Zero;
                    }
                }

                lock (_initLock)
                {
                    if (_refCount > 0)
                    {
                        _refCount--;
                        if (_refCount == 0)
                        {
                            finalize_lib_tscan();
                        }
                    }
                }
            }
        }

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern double tscan_get_bus_status(IntPtr ADeviceHandle, CHANNEL_INDEX AChnBase0, TSTATISTICTYPE AIndex);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void tscan_set_auto_calc_bus_statistics(bool Value);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void tscan_clear_can_bus_statistic();

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_connect(string ADeviceSerial, ref IntPtr ADeviceHandle);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_disconnect_all_devices();

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_disconnect_by_serial(string ADeviceSerial);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_disconnect_by_handle(IntPtr ADeviceHandle);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_get_error_description(uint ACode, IntPtr ADesc);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_register_event_can(IntPtr ADeviceHandle, TCANQueueEvent_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_unregister_event_can(IntPtr ADeviceHandle, TCANQueueEvent_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_register_event_canfd(IntPtr ADeviceHandle, TCANFDQueueEvent_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_unregister_event_canfd(IntPtr ADeviceHandle, TCANFDQueueEvent_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tslin_register_event_lin(IntPtr ADeviceHandle, TLINQueueEvent_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tslin_unregister_event_lin(IntPtr ADeviceHandle, TLINQueueEvent_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_transmit_can_sync(IntPtr ADeviceHandle, ref TLIBCAN ACAN, uint ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_transmit_can_async(IntPtr ADeviceHandle, ref TLIBCAN ACAN);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_add_cyclic_msg_can(IntPtr ADeviceHandle, ref TLIBCAN ACAN, float APeriodMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_delete_cyclic_msg_can(IntPtr ADeviceHandle, ref TLIBCAN ACAN);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern uint tsfifo_receive_can_msgs(IntPtr ADeviceHandle, IntPtr ACANBuffer, ref int ABufferSize, CHANNEL_INDEX AChn, READ_TX_RX_DEF ATxRx);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_clear_can_receive_buffers(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIndex);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsfifo_read_can_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsfifo_read_can_tx_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsfifo_read_can_rx_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsfifo_read_canfd_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsfifo_read_canfd_tx_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsfifo_read_canfd_rx_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_read_lin_buffer_datacount(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_read_lin_tx_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_read_lin_rx_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_read_fastlin_buffer_datacount(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_read_fastlin_tx_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_read_fastlin_rx_buffer_frame_count(IntPtr ADeviceHandle, CHANNEL_INDEX AChannel, ref int ACount);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_transmit_canfd_sync(IntPtr ADeviceHandle, ref TLIBCANFD ACANFD, uint ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_transmit_canfd_async(IntPtr ADeviceHandle, ref TLIBCANFD ACANFD);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_add_cyclic_msg_canfd(IntPtr ADeviceHandle, ref TLIBCANFD ACANFD, float APeriodMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_delete_cyclic_msg_canfd(IntPtr ADeviceHandle, ref TLIBCANFD ACANFD);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern uint tsfifo_receive_canfd_msgs(IntPtr ADeviceHandle, IntPtr ACANFDBuffers, ref int ACANFDBufferSize, CHANNEL_INDEX AChn, READ_TX_RX_DEF ATxRx);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_clear_canfd_receive_buffers(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIndex);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_add_can_canfd_pass_filter(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, int AIdentifier, bool AIsStd);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_add_lin_pass_filter(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, int AIdentifier);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_delete_can_canfd_pass_filter(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, int AIdentifier);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_delete_lin_pass_filter(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, int AIdentifier);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_config_can_by_baudrate(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, double ARateKbp, uint A120OhmConnected);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_config_canfd_by_baudrate(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, double AArbRateKbps, double ADataRateKbps, TLIBCANFDControllerType AControllerType, TLIBCANFDControllerMode AControllerMode, uint A120OhmConnected);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tslin_transmit_lin_sync(IntPtr ADeviceHandle, ref TLIBLIN ALIN, uint ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tslin_transmit_lin_async(IntPtr ADeviceHandle, ref TLIBLIN ALIN);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern uint tsfifo_receive_lin_msgs(IntPtr ADeviceHandle, IntPtr ALINBuffer, ref int ABufferSize, CHANNEL_INDEX AChn, READ_TX_RX_DEF ATxRx);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_clear_lin_receive_buffers(IntPtr ADeviceHandle, CHANNEL_INDEX AChn);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern uint tsfifo_receive_fastlin_msgs(IntPtr ADeviceHandle, IntPtr AFastLINBuffer, ref int ABufferSize, CHANNEL_INDEX AChn, READ_TX_RX_DEF ATxRx);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tsfifo_clear_fastlin_receive_buffers(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIndex);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_get_error_description(uint ACode, ref IntPtr ADesc);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_register_event_connected(TTSCANConnectedCallback_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_register_event_disconnected(TTSCANConnectedCallback_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_unregister_event_connected(TTSCANConnectedCallback_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_unregister_event_disconnected(TTSCANConnectedCallback_Win32 ACallback);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsccp_apply_read_data_package(IntPtr ADeviceHandle, uint AMasterCRO, uint ASlaveDTO, int ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsccp_read_data_package(IntPtr ADeviceHandle, byte AChn, byte AIsStd, byte isFirstPackage, byte ACTRNum, uint AMasterCRO, uint ASlaveDTO, byte* pData, int dataCnt, int ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tslin_config_baudrate(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, double ARateKbps, LIN_PROTOCOL AProtocol);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tslin_set_schedule_table(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte ASchIndex);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tslin_stop_lin_channel(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tslin_set_node_funtiontype(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, T_LIN_NODE_FUNCTION AFunctionType);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern uint tstp_lin_master_request(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte ANAD, byte* AData, ushort ADataNum, uint ATimeoutMs);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern uint tstp_lin_master_tranceive_sync(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte AReqNAD, byte* AReqData, int AReqDataNum, byte* AResNAD, byte* AResData, int* AResDataNum, uint ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tstp_lin_master_request_intervalms(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte AData);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tslin_clear_schedule_tables(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tstp_lin_reset(IntPtr ADeviceHandle, uint AChnIdx);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tstp_lin_slave_response_intervalms(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte AData);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tstp_lin_tp_para_default(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, ushort AReqIntervalMs, ushort AResIntervalMs, ushort AResRetryTime);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tstp_lin_tp_para_special(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, ushort AReqIntervalMs, ushort AResIntervalMs, ushort AResRetryTime);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsdiag_lin_read_data_by_identifier(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte ANAD, ushort AId, byte* AResNAD, byte* AResData, int* AResDataNum, uint ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsdiag_lin_write_data_by_identifier(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte ANAD, ushort AId, byte* AReqData, int AReqDataNum, byte* AResNAD, byte* AResData, int* AResDataNum, uint ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_lin_session_control(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte ANAD, byte ASession, uint ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsdiag_lin_fault_memory_read(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte ANAD, ushort data_length, byte* AData, uint ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsdiag_lin_fault_memory_clear(IntPtr ADeviceHandle, CHANNEL_INDEX AChnIdx, byte ANAD, ushort data_length, byte* AData, uint ATimeoutMS);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_can_create(ref int pDiagModuleIndex, CHANNEL_INDEX AChnIndex, byte ASupportFDCAN, byte AMaxDLC, uint ARequestID, bool ARequestIDIsStd, uint AResponseID, bool AResponseIDIsStd, uint AFunctionID, bool AFunctionIDIsStd);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_can_delete(int ADiagModuleIndex);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern void tsdiag_can_delete_all();

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_can_attach_to_tscan_tool(int ADiagModuleIndex, IntPtr ACANToolHandle);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tstp_can_register_tx_completed_recall(int ADiagModuleIndex, N_USData_TranslateCompleted_Recall ATxcompleted);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tstp_can_register_rx_completed_recall(int ADiagModuleIndex, N_USData_TranslateCompleted_Recall ARxcompleted);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tstp_can_send_functional(int ADiagModuleIndex, byte* AReqDataArray, int AReqDataSize);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tstp_can_send_request(int ADiagModuleIndex, byte* AReqDataArray, int AReqDataSize);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tstp_can_request_and_get_response(int ADiagModuleIndex, byte* AReqDataArray, int AReqDataSize, byte* AResponseDataArray, ref int AResponseDataSize);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_can_session_control(int ADiagModuleIndex, byte ASubSession);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_can_routine_control(int ADiagModuleIndex, byte ARoutineControlType, ushort ARoutintID);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_can_communication_control(int ADiagModuleIndex, byte AControlType);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsdiag_can_security_access_request_seed(int ADiagModuleIndex, int ALevel, byte* ARecSeed, ref int ARecSeedSize);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsdiag_can_security_access_send_key(int ADiagModuleIndex, int ALevel, byte* AKeyValue, int AKeySize);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_can_request_download(int ADiagModuleIndex, uint AMemAddr, uint AMemSize);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_can_request_upload(int ADiagModuleIndex, uint AMemAddr, uint AMemSize);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsdiag_can_transfer_data(int ADiagModuleIndex, byte* ASourceDatas, int ADataSize, int AReqCase);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int tsdiag_can_request_transfer_exit(int ADiagModuleIndex);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsdiag_can_write_data_by_identifier(int ADiagModuleIndex, ushort ADataIdentifier, byte* AWriteData, int AWriteDataSize);

        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public unsafe static extern int tsdiag_can_read_data_by_identifier(int ADiagModuleIndex, ushort ADataIdentifier, byte* AReturnArray, ref int AReturnArraySize);
        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_configure_can_regs(IntPtr intPtr, CHANNEL_INDEX cHANNEL_INDEX, double ABaudrateKbps, int ASEG1, int ASEG2, int APrescaler, int ASJ2, bool AOnlyListen, bool A120);
        [DllImport(TSCANDLL_NAME, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern uint tscan_configure_canfd_regs(IntPtr intPtr, CHANNEL_INDEX cHANNEL_INDEX, double AArbRateKbps, int ASEGI, int ASEG2, int APrescaller, int ASJ2, double ADataRateKbps, int ADataSEG1, int ADataSEG2, int ADataPrescaler, int ADataSJ2, TLIBCANFDControllerType tLIBCANFDControllerType, TLIBCANFDControllerMode tLIBCANFDControllerMode, bool A120);


        public unsafe static int tsdiag_can_security_access_request_seed_dontnet(int ADiagModuleIndex, int ALevel, ref uint ARecSeed)
        {
            int ARecSeedSize = 4;
            uint num = 0u;
            byte* aRecSeed = (byte*)(&num);
            int result = tsdiag_can_security_access_request_seed(ADiagModuleIndex, ALevel, aRecSeed, ref ARecSeedSize);
            ARecSeed = num;
            return result;
        }

        public unsafe static int tsdiag_can_security_access_send_key_dontnet(int ADiagModuleIndex, int ALevel, uint AKeyValue)
        {
            byte* aKeyValue = (byte*)(&AKeyValue);
            return tsdiag_can_security_access_send_key(ADiagModuleIndex, ALevel, aKeyValue, 4);
        }

        public static uint GetDeviceInfo(uint ADeviceIndex, ref string AFManufacturer, ref string AFProduct, ref string AFSerial)
        {
            IntPtr AFManufacturer2 = (IntPtr)0;
            IntPtr AFProduct2 = (IntPtr)0;
            IntPtr AFSerial2 = (IntPtr)0;
            uint num = tscan_get_device_info(ADeviceIndex, ref AFManufacturer2, ref AFProduct2, ref AFSerial2);
            if (num == 0)
            {
                AFManufacturer = Marshal.PtrToStringAnsi(AFManufacturer2);
                AFProduct = Marshal.PtrToStringAnsi(AFProduct2);
                AFSerial = Marshal.PtrToStringAnsi(AFSerial2);
            }

            return num;
        }

        public static string TSCAN_GetTSCANErrorDescription(uint ACode)
        {
            IntPtr ADesc = (IntPtr)0;
            if (tscan_get_error_description(ACode, ref ADesc) != 0)
            {
                return "";
            }

            return Marshal.PtrToStringAnsi(ADesc);
        }

        public static bool ReceiveCANMsg(IntPtr ADeviceHandle, out TLIBCAN ACANMsg, int ATimeoutMs, CHANNEL_INDEX AChn = CHANNEL_INDEX.CHN1, READ_TX_RX_DEF AOnlyRx = READ_TX_RX_DEF.ONLY_RX_MESSAGES)
        {
            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TLIBCAN)));
            DateTime now = DateTime.Now;
            bool result = false;
            ACANMsg = default(TLIBCAN);
            try
            {
                do
                {
                    int ABufferSize = 1;
                    if (tsfifo_receive_can_msgs(ADeviceHandle, intPtr, ref ABufferSize, AChn, AOnlyRx) == 0 && ABufferSize > 0)
                    {
                        ACANMsg = (TLIBCAN)Marshal.PtrToStructure(intPtr, typeof(TLIBCAN));
                        result = true;
                        break;
                    }

                    Thread.Sleep(1);
                }
                while ((DateTime.Now - now).TotalMilliseconds < (double)ATimeoutMs);
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }

            return result;
        }

        public static int ReceiveCANMsgList(IntPtr ADeviceHandle, ref TLIBCAN[] ACANMsgBuffer, int ACANBufferSize, CHANNEL_INDEX AChn = CHANNEL_INDEX.CHN1, READ_TX_RX_DEF ATxRx = READ_TX_RX_DEF.ONLY_RX_MESSAGES)
        {
            IntPtr intPtr = Marshal.AllocHGlobal((IntPtr)(Marshal.SizeOf(typeof(TLIBCAN)) * ACANBufferSize));
            DateTime now = DateTime.Now;
            int ABufferSize = ACANBufferSize;
            try
            {
                tsfifo_receive_can_msgs(ADeviceHandle, intPtr, ref ABufferSize, AChn, ATxRx);
                for (int i = 0; i < ABufferSize; i++)
                {
                    ACANMsgBuffer[i] = default(TLIBCAN);
                    ACANMsgBuffer[i] = (TLIBCAN)Marshal.PtrToStructure(intPtr + Marshal.SizeOf(typeof(TLIBCAN)) * i, typeof(TLIBCAN));
                }

                return ABufferSize;
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }
        }

        public static bool ReceiveCANFDMsg(IntPtr ADeviceHandle, out TLIBCANFD ACANMsg, int ATimeoutMs, CHANNEL_INDEX AChn = CHANNEL_INDEX.CHN1, READ_TX_RX_DEF ARxTx = READ_TX_RX_DEF.ONLY_RX_MESSAGES)
        {
            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TLIBCANFD)));
            DateTime now = DateTime.Now;
            bool result = false;
            ACANMsg = default(TLIBCANFD);
            try
            {
                do
                {
                    int ACANFDBufferSize = 1;
                    if (tsfifo_receive_canfd_msgs(ADeviceHandle, intPtr, ref ACANFDBufferSize, AChn, ARxTx) == 0 && ACANFDBufferSize > 0)
                    {
                        ACANMsg = (TLIBCANFD)Marshal.PtrToStructure(intPtr, typeof(TLIBCANFD));
                        result = true;
                        break;
                    }

                    Thread.Sleep(1);
                }
                while ((DateTime.Now - now).TotalMilliseconds < (double)ATimeoutMs);
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }

            return result;
        }

        public static int ReceiveCANFDMsgList(IntPtr ADeviceHandle, ref TLIBCANFD[] ACANMsgBuffer, int ACANBufferSize, CHANNEL_INDEX AChn = CHANNEL_INDEX.CHN1, READ_TX_RX_DEF ARxTx = READ_TX_RX_DEF.ONLY_RX_MESSAGES)
        {
            IntPtr intPtr = Marshal.AllocHGlobal((IntPtr)(Marshal.SizeOf(typeof(TLIBCANFD)) * ACANBufferSize));
            DateTime now = DateTime.Now;
            int ACANFDBufferSize = ACANBufferSize;
            try
            {
                if (tsfifo_receive_canfd_msgs(ADeviceHandle, intPtr, ref ACANFDBufferSize, AChn, ARxTx) == 0)
                {
                    for (int i = 0; i < ACANFDBufferSize; i++)
                    {
                        ACANMsgBuffer[i] = default(TLIBCANFD);
                        ACANMsgBuffer[i] = (TLIBCANFD)Marshal.PtrToStructure(intPtr + Marshal.SizeOf(typeof(TLIBCANFD)) * i, typeof(TLIBCANFD));
                    }
                }
                else
                {
                    ACANFDBufferSize = 0;
                }

                return ACANFDBufferSize;
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }
        }

        public static bool ReceiveLINMsg(IntPtr ADeviceHandle, out TLIBLIN ALINMsg, int ATimeoutMs, CHANNEL_INDEX AChn = CHANNEL_INDEX.CHN1, READ_TX_RX_DEF ATxRx = READ_TX_RX_DEF.ONLY_RX_MESSAGES)
        {
            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TLIBLIN)));
            DateTime now = DateTime.Now;
            bool result = false;
            ALINMsg = default(TLIBLIN);
            try
            {
                do
                {
                    int ABufferSize = 1;
                    if (tsfifo_receive_lin_msgs(ADeviceHandle, intPtr, ref ABufferSize, AChn, ATxRx) == 0 && ABufferSize > 0)
                    {
                        ALINMsg = (TLIBLIN)Marshal.PtrToStructure(intPtr, typeof(TLIBLIN));
                        result = true;
                        break;
                    }

                    Thread.Sleep(1);
                }
                while ((DateTime.Now - now).TotalMilliseconds < (double)ATimeoutMs);
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }

            return result;
        }

        public static int ReceiveLINMsgList(IntPtr ADeviceHandle, ref TLIBLIN[] ALINMsgBuffer, int ALINBufferSize, CHANNEL_INDEX AChn = CHANNEL_INDEX.CHN1, READ_TX_RX_DEF ATxRx = READ_TX_RX_DEF.ONLY_RX_MESSAGES)
        {
            IntPtr intPtr = Marshal.AllocHGlobal((IntPtr)(Marshal.SizeOf(typeof(TLIBLIN)) * ALINBufferSize));
            DateTime now = DateTime.Now;
            int ABufferSize = ALINBufferSize;
            try
            {
                if (tsfifo_receive_lin_msgs(ADeviceHandle, intPtr, ref ABufferSize, AChn, ATxRx) == 0)
                {
                    for (int i = 0; i < ABufferSize; i++)
                    {
                        ALINMsgBuffer[i] = default(TLIBLIN);
                        ALINMsgBuffer[i] = (TLIBLIN)Marshal.PtrToStructure(intPtr + Marshal.SizeOf(typeof(TLIBLIN)) * i, typeof(TLIBLIN));
                    }
                }
                else
                {
                    ABufferSize = 0;
                }

                return ABufferSize;
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }
        }

        public static bool ReceiveFastLINMsg(IntPtr ADeviceHandle, out TLIBLIN ALINMsg, int ATimeoutMs, CHANNEL_INDEX AChn = CHANNEL_INDEX.CHN1, READ_TX_RX_DEF AOnlyRx = READ_TX_RX_DEF.ONLY_RX_MESSAGES)
        {
            IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TLIBLIN)));
            DateTime now = DateTime.Now;
            bool result = false;
            ALINMsg = default(TLIBLIN);
            try
            {
                do
                {
                    int ABufferSize = 1;
                    if (tsfifo_receive_fastlin_msgs(ADeviceHandle, intPtr, ref ABufferSize, AChn, AOnlyRx) == 0 && ABufferSize > 0)
                    {
                        ALINMsg = (TLIBLIN)Marshal.PtrToStructure(intPtr, typeof(TLIBLIN));
                        result = true;
                        break;
                    }

                    Thread.Sleep(1);
                }
                while ((DateTime.Now - now).TotalMilliseconds < (double)ATimeoutMs);
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }

            return result;
        }

        public static int ReceiveFastLINMsgList(IntPtr ADeviceHandle, ref TLIBLIN[] ALINMsgBuffer, int ALINBufferSize, CHANNEL_INDEX AChn = CHANNEL_INDEX.CHN1, READ_TX_RX_DEF AOnlyRx = READ_TX_RX_DEF.ONLY_RX_MESSAGES)
        {
            IntPtr intPtr = Marshal.AllocHGlobal((IntPtr)(Marshal.SizeOf(typeof(TLIBLIN)) * ALINBufferSize));
            DateTime now = DateTime.Now;
            int ABufferSize = ALINBufferSize;
            try
            {
                if (tsfifo_receive_fastlin_msgs(ADeviceHandle, intPtr, ref ABufferSize, AChn, AOnlyRx) == 0)
                {
                    for (int i = 0; i < ABufferSize; i++)
                    {
                        ALINMsgBuffer[i] = default(TLIBLIN);
                        ALINMsgBuffer[i] = (TLIBLIN)Marshal.PtrToStructure(intPtr + Marshal.SizeOf(typeof(TLIBLIN)) * i, typeof(TLIBLIN));
                    }
                }
                else
                {
                    ABufferSize = 0;
                }

                return ABufferSize;
            }
            finally
            {
                Marshal.FreeHGlobal(intPtr);
            }
        }



    }
}
