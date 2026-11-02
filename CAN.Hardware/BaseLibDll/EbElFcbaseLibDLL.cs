using System.Runtime.InteropServices;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// EbEl(Eberspächer Electronics) FlexCard API(fcbase.dll) 互操作层。
    /// fcbase.dll 随 FlexCard 驱动安装进系统(DLL 搜索路径),32/64 位进程同名,
    /// 工程不随带 DLL。与 ESD/ETAS 一样走 LoadLibrary 动态加载,
    /// 9 个导出函数全部桥接为委托(调用约定 StdCall,与参考实现 createDelegate 默认一致)。
    /// 注意:该 API 只有经典 CAN(FlexCard 为 FlexRay/CAN 时代硬件,无 CAN FD);
    /// 发送是"预配置报文缓冲"模型(fcbCANTransmit 只给缓冲号 + 数据,不带 CAN ID)。
    /// </summary>
    internal static class EbElFcbaseLibDLL
    {
        // fcbase.dll 由 FlexCard 驱动安装,使用裸 DLL 名由系统搜索路径解析
        private const string DLL_NAME = "fcbase.dll";

        /// <summary>FlexCard 原生扩展帧标记(bit29),与本工程托管层 SocketCAN 风格 bit31(CanFrameDef.CAN_EFF_FLAG)不同,需双向换乘</summary>
        internal const uint FC_EXT_FLAG = 0x20000000;

        #region 枚举

        /// <summary>fcbase 返回码, 0=OK</summary>
        internal enum fcResult
        {
            OK = 0,
            INTERNAL_ERROR = 99,
            RESERVED = 100,
            NULL_PARAMETER,
            INVALID_PARAMETER,
            MEMORY_ALLOCATION_FAILED,
            INVALID_OBJECT_HANDLE,
            ACTION_NOT_SUPPORTED,
            PARAMETER_NOT_SUPPORTED,
            ACTION_FAILED,
            ACTION_NOT_ALLOWED_DURING_MONITORING,
            TEXT_NOT_DEFINIED,
            CONNECTION_ALREADY_OPEN,
            UNKOWN_FLEXCARD_ID,
            MONITORING_IS_ALREADY_ACTIVE,
            REGISTER_NOT_READABLE,
            REGISTER_NOT_WRITEABLE,
            PARSING_ERROR,
            INCOMPATIBLE_VERSION,
            CONFIGURATION_FAILED,
            RX_BUFFER_CANNOT_USED,
            INVALID_MSGBUF_SETTINGS,
            MINIMUM_FIFO_LIMIT,
            NOT_ENOUGH_MESSAGE_RAM,
            PAYLOAD_EXCEEDS_MAXIMUM,
            MSG_BUF_BUSY,
            MSG_BUF_LOCKED_FOR_TRANSMISSION,
            INCORRECT_POC_STATE,
            FIRMWARE_UPDATE_REQUIRED,
            FUNCTION_NOT_IMPLEMENTED,
            CONNECTION_RELEASED_AFTER_SURPRISE_REMOVAL,
            CC_INDEX_NOT_VALID,
            PMC_CARD_FUNCTION,
            TRIGGER_FUNCTION_VERSION_CONFLICT,
            XENOMAI_TASK_UNBLOCKED,
            XENOMAI_EVENT_DESTROYED,
            XENOMAI_TIMEDOUT,
            XENOMAI_ILLEGAL_INVOCATION,
            XENOMAI_NO_REALTIME_CONTEXT,
            CARDBUS_CARD_FUNCTION,
            DRIVER_UPDATE_REQUIRED,
            CC_CAN_NOT_AVAILABLE,
            VERSION_NOT_MATCH,
            INVALID_CCCONFIG_SETTINGS,
            BUS_TYPE_NOT_VALID,
            SELFSYNC_NOT_AVAILABLE,
            CAN_BUFFER_NOT_SET_YET,
            INVALID_HARDWARE_LICENSE,
            PMCII_CARD_FUNCTION,
            PCI_BASED_CARD_FUNCTION,
            USB_FULL_SPEED_UNSUPPORTED,
            TX_FIFO_NOT_CONFIGURED,
            TX_FIFO_FULL,
            TX_FIFO_MSGBUF_NR,
        }

        /// <summary>报文包类型(fcbReceive 链表节点 fcPacket.Type)</summary>
        internal enum fcPacketType
        {
            Info,
            FlexRayFrame,
            Error,
            Status,
            Trigger,
            TxAcknowledge,
            NMVector,
            Notification,
            TriggerEx,
            CAN,
            CANError,
        }

        /// <summary>通信控制器通道号(CC1~CC8),配置里的 Channel(0基) 即该枚举值</summary>
        internal enum fcCC
        {
            CC1, CC2, CC3, CC4, CC5, CC6, CC7, CC8,
        }

        /// <summary>fcCANPacket.FlagsAndDLC 字节位表: bit0-3=DLC(直接是字节数), bit4=方向, bit5=RTR, bit6=丢帧</summary>
        [Flags]
        internal enum DLCFlags : byte
        {
            Direction = 0x10,       // 0=接收, 1=发送确认(TxAck)
            RemoteFrame = 0x20,
            MessageLost = 0x40,
            Reserved = 0x80,
        }

        /// <summary>fcFreeMemory 的内存类型(驱动内部分配,用完必须归还)</summary>
        internal enum fcMemoryType
        {
            String,
            Info,
            Packet,
            InfoV2,
            InfoHwSw,
        }

        /// <summary>CAN 监控模式(本封装只用 Normal)</summary>
        internal enum fcCANMonitoringMode
        {
            Normal = 0,
            Active = Normal,
            Silent = 1,
            Passive = Silent,
        }

        /// <summary>FlexCard 设备型号(fcbGetEnumFlexCardsV3 枚举结果)</summary>
        internal enum fcFlexCardDeviceId
        {
            NoDevice = 0,
            FlexCardCycloneII = 5,
            FlexCardCycloneIIPXI = 6,
            FlexCardPMC = 7,
            FlexCardCycloneIISE = 8,
            FlexCardPMCII = 9,
            FlexXConMidgetBinderV1 = 10,
            FlexXConMidgetBinderV2 = 11,
            FlexXConMidgetLemo = 12,
            FlexXConExpert = 13,
            FlexCardUSB = 14,
            FlexXConCompact = 15,
            PCIeNGP = 16,
        }

        #endregion

        #region 结构体(全部 Pack=1; C 枚举按 int 编组)

        /// <summary>CAN 报文(22 字节): fcbReceive 链表中 pCANPacket 指向的负载</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct fcCANPacket
        {
            public uint ID;                 // 11/29 位 CAN ID, 扩展帧用 bit29(FC_EXT_FLAG)标记
            public uint TimeStamp;          // 硬件时间戳(原始 tick, 单位未公开, 透传不换算)
            public byte BufferNumber;
            public DLCFlags FlagsAndDLC;    // bit0-3=DLC, bit4=Direction, bit5=RTR, bit6=MessageLost
            public fcCC CC;                 // C 枚举按 int 编组(4 字节)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data;
        }

        /// <summary>接收链表节点(x64 20 字节): fcbReceive 返回一串, pNextPacket 链接</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct fcPacket
        {
            public fcPacketType Type;
            public IntPtr pCANPacket;
            public IntPtr pNextPacket;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct fcVersionNumber
        {
            public uint Major;
            public uint Minor;
            public uint Update;
            public uint Release;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct fcInfoSw
        {
            public fcVersionNumber VersionBaseDll;
            public fcVersionNumber VersionDeviceDriver;
            public uint LicenseFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            private uint[] Reserved;
        }

        /// <summary>各类总线控制器数量</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct fcNumberCC
        {
            public byte FlexRay;
            public byte CAN;
            public byte LIN;
            public byte MOST;
            public byte FlexRaySelfSync;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private byte[] Reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct fcInfoHw
        {
            public ulong Serial;
            public fcFlexCardDeviceId DeviceId;
            public fcVersionNumber VersionFirmware;
            public fcVersionNumber VersionHardware;
            public fcNumberCC SupportedCCs;
            public fcNumberCC LicensedCCs;
            public fcNumberCC UseableCCs;
            public IntPtr pVersionCC;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private uint[] Reserved;
        }

        /// <summary>设备枚举链表节点: fcbGetEnumFlexCardsV3 返回一串, pNext 链接</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct fcInfoHwSw
        {
            public uint FlexCardId;
            public uint UserDefinedCardId;
            public fcInfoSw InfoSoftware;
            public fcInfoHw InfoHardware;
            public uint Busy;
            public IntPtr pNext;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private uint[] Reserved;
        }

        /// <summary>CAN 位时序配置(36 字节): 波特率只能靠"分频器 + 位段"手工组</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct fcCANCcConfig
        {
            public ushort BaudRatePrescaler;
            public ushort SynchronizationJumpWidth;
            public ushort TimeSegment1;
            public ushort TimeSegment2;
            public uint EnableAutomaticRetransmission;   // 1=使能自动重发(参考实现遗留 0=关闭, 相当于单次发送)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            private uint[] Reserved;
        }

        #endregion

        #region 委托声明(全部 StdCall, 与参考实现 createDelegate 默认一致)

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate fcResult GetEnumFlexCardsV3Func(ref IntPtr pInfoHwSw, byte getBusyDevices);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate fcResult FreeMemoryFunc(fcMemoryType memoryType, IntPtr p);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate fcResult OpenFunc(ref IntPtr phFlexCard, uint flexCardId);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate fcResult CloseFunc(IntPtr phFlexCard);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate fcResult CANMonitoringStartFunc(IntPtr hFlexCard, fcCC cc, byte resetTimestamps, fcCANMonitoringMode mode);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate fcResult CANMonitoringStopFunc(IntPtr hFlexCard, fcCC cc);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate fcResult CANTransmitFunc(IntPtr hFlexCard, fcCC cc, uint bufferNumber, byte[] data, byte transmitNewData, byte ignoreTxRqstLock);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate fcResult ReceiveFunc(IntPtr hFlexCard, ref IntPtr pPacket);
        // 原生签名未公开, 按参考实现按值传递(36 字节结构体; x64 ABI 下大结构体按隐藏引用传递)
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate fcResult CANSetCcConfigurationFunc(IntPtr hFlexCard, fcCC cc, fcCANCcConfig cfg);

        #endregion

        #region 动态加载

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private static readonly object _loadLock = new object();
        private static IntPtr _hModule = IntPtr.Zero;

        private static GetEnumFlexCardsV3Func? _getEnumFlexCardsV3;
        private static FreeMemoryFunc? _freeMemory;
        private static OpenFunc? _open;
        private static CloseFunc? _close;
        private static CANMonitoringStartFunc? _monitoringStart;
        private static CANMonitoringStopFunc? _monitoringStop;
        private static CANTransmitFunc? _transmit;
        private static ReceiveFunc? _receive;
        private static CANSetCcConfigurationFunc? _setCcConfiguration;

        /// <summary>定位并加载 fcbase.dll, 解析全部导出函数; 重复调用为空操作</summary>
        internal static void EnsureLoaded()
        {
            if (_hModule != IntPtr.Zero) return;
            lock (_loadLock)
            {
                if (_hModule != IntPtr.Zero) return;

                IntPtr hModule = LoadLibrary(DLL_NAME);
                if (hModule == IntPtr.Zero)
                    throw new DllNotFoundException($"未检测到 EbEl FlexCard 驱动(未找到 {DLL_NAME}),请先安装 Eberspächer Electronics FlexCard 驱动(fcbase.dll,随 FlexCard 硬件发布)");

                _getEnumFlexCardsV3 = GetDelegate<GetEnumFlexCardsV3Func>(hModule, "fcbGetEnumFlexCardsV3");
                _freeMemory = GetDelegate<FreeMemoryFunc>(hModule, "fcFreeMemory");
                _open = GetDelegate<OpenFunc>(hModule, "fcbOpen");
                _close = GetDelegate<CloseFunc>(hModule, "fcbClose");
                _monitoringStart = GetDelegate<CANMonitoringStartFunc>(hModule, "fcbCANMonitoringStart");
                _monitoringStop = GetDelegate<CANMonitoringStopFunc>(hModule, "fcbCANMonitoringStop");
                _transmit = GetDelegate<CANTransmitFunc>(hModule, "fcbCANTransmit");
                _receive = GetDelegate<ReceiveFunc>(hModule, "fcbReceive");
                _setCcConfiguration = GetDelegate<CANSetCcConfigurationFunc>(hModule, "fcbCANSetCcConfiguration");

                // 关键函数缺失时立即报错,避免后续空调用 NullReferenceException(参考实现此处静默返回 null)
                if (_getEnumFlexCardsV3 == null || _freeMemory == null || _open == null || _close == null
                    || _monitoringStart == null || _monitoringStop == null || _transmit == null
                    || _receive == null || _setCcConfiguration == null)
                {
                    throw new EntryPointNotFoundException($"{DLL_NAME} 导出函数不完整(fcbGetEnumFlexCardsV3/fcFreeMemory/fcbOpen/fcbClose/fcbCANMonitoringStart/fcbCANMonitoringStop/fcbCANTransmit/fcbReceive/fcbCANSetCcConfiguration),请确认安装的是完整的 FlexCard 驱动");
                }

                // 全部解析成功后提交模块句柄(进程内不卸载)
                _hModule = hModule;
            }
        }

        private static T? GetDelegate<T>(IntPtr hModule, string name) where T : class
        {
            IntPtr proc = GetProcAddress(hModule, name);
            return proc == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(proc);
        }

        #endregion

        #region 对外 API

        /// <summary>
        /// 枚举本机 FlexCard 设备,返回第一块带 CAN 控制器的卡 ID(0 表示未找到)。
        /// 驱动返回的是 fcInfoHwSw 链表,用完必须 fcFreeMemory(InfoHwSw) 归还。
        /// </summary>
        internal static uint FindFirstCanCard()
        {
            EnsureLoaded();
            var pInfoHwSw = IntPtr.Zero;
            try
            {
                var result = _getEnumFlexCardsV3!(ref pInfoHwSw, 1);
                if (result != fcResult.OK || pInfoHwSw == IntPtr.Zero)
                    return 0;

                var pCurrent = pInfoHwSw;
                while (pCurrent != IntPtr.Zero)
                {
                    var info = Marshal.PtrToStructure<fcInfoHwSw>(pCurrent);
                    if (info.FlexCardId != 0
                        && info.InfoHardware.DeviceId != fcFlexCardDeviceId.NoDevice
                        && info.InfoHardware.SupportedCCs.CAN > 0)
                        return info.FlexCardId;
                    pCurrent = info.pNext;
                }
                return 0;
            }
            finally
            {
                if (pInfoHwSw != IntPtr.Zero)
                    _freeMemory!(fcMemoryType.InfoHwSw, pInfoHwSw);
            }
        }

        internal static fcResult Open(uint flexCardId, out IntPtr handle)
        {
            EnsureLoaded();
            handle = IntPtr.Zero;
            return _open!(ref handle, flexCardId);
        }

        internal static fcResult Close(IntPtr handle)
        {
            EnsureLoaded();
            return _close!(handle);
        }

        internal static fcResult MonitoringStart(IntPtr handle, fcCC cc)
        {
            EnsureLoaded();
            return _monitoringStart!(handle, cc, 1, fcCANMonitoringMode.Normal);
        }

        internal static fcResult MonitoringStop(IntPtr handle, fcCC cc)
        {
            EnsureLoaded();
            return _monitoringStop!(handle, cc);
        }

        /// <summary>fcbCANTransmit: 缓冲式发送(只给缓冲号 + 数据, CAN ID 由报文缓冲的既有配置决定)</summary>
        internal static fcResult Transmit(IntPtr handle, fcCC cc, uint bufferNumber, byte[] data)
        {
            EnsureLoaded();
            return _transmit!(handle, cc, bufferNumber, data, 1, 0);
        }

        /// <summary>fcbReceive: 取一串报文包(链表), 无报文时 pPacket 为 Zero; 取得后由调用方 fcFreeMemory(Packet) 归还</summary>
        internal static fcResult Receive(IntPtr handle, out IntPtr pPacket)
        {
            EnsureLoaded();
            pPacket = IntPtr.Zero;
            return _receive!(handle, ref pPacket);
        }

        /// <summary>归还 fcbReceive 取得的报文包链表</summary>
        internal static void FreePacket(IntPtr pPacket)
        {
            EnsureLoaded();
            _freeMemory!(fcMemoryType.Packet, pPacket);
        }

        internal static fcResult SetCcConfiguration(IntPtr handle, fcCC cc, fcCANCcConfig cfg)
        {
            EnsureLoaded();
            return _setCcConfiguration!(handle, cc, cfg);
        }

        #endregion
    }
}
