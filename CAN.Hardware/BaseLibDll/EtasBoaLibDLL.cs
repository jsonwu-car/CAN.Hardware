using System.Runtime.InteropServices;
using System.Text;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// ETAS BOA V2(Boa Open Api) 互操作层。
    /// BOA 由两个原生 DLL 组成: dll-ocdProxy.dll(OCI, Open Controller Interface, CAN/LIN 控制与收发)
    /// 与 dll-csiBind.dll(CSI, Common Service Interface, 硬件枚举)。
    /// 两个 DLL 随 ETAS BOA V2 驱动安装(如随 INCA 安装)到 Program Files 目录,不随工程携带;
    /// 由于安装目录不在系统 DLL 搜索路径内,且 BOA DLL 的依赖项与其同目录,
    /// 这里采用 注册表/默认路径定位 + 进程 PATH 追加 + LoadLibrary 动态加载(参考 python-can 的 ETAS 绑定做法)。
    /// 调用约定均为 Cdecl。
    /// </summary>
    internal static class EtasBoaLibDLL
    {
        // ---------------- DLL 定位与动态加载 ----------------

        private const string BOA_DLL = "dll-ocdProxy.dll";
        private const string CSI_DLL = "dll-csiBind.dll";
        private const string BOA_REG_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\ETAS\BOA\2";
        private const string BOA_PATH_FMT = @"ETAS\BOA_V2\Bin\{0}\Dll\Framework";

        // BOA 控制器 URI 前缀, 如 "ETAS:/" + "/ETAS.ETH://ES581.4/CAN:1"
        internal const string URI_PREFIX = "ETAS:/";

        // 无效句柄
        internal const int UNDEFINED = -1;

        // OCI CAN API 版本(固定协商 1.3)
        internal static readonly EtasVersion ApiV13 = new EtasVersion { major = 1, minor = 3 };

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string filename);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);

        private static IntPtr _boaModule = IntPtr.Zero;
        private static IntPtr _csiModule = IntPtr.Zero;
        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        /// <summary>
        /// 定位 BOA 安装目录: 先查注册表 HKLM\SOFTWARE\ETAS\BOA\2 的 PATH 值, 再回退默认安装路径
        /// </summary>
        private static string ResolveBoaDirectory()
        {
            try
            {
                var installedPath = Microsoft.Win32.Registry.GetValue(BOA_REG_KEY, "PATH", null) as string;
                if (!string.IsNullOrEmpty(installedPath))
                {
                    // 注册表路径可能指向与本进程位数不一致的目录, 按进程位数修正
                    if (Environment.Is64BitProcess && installedPath.Contains("win32"))
                        installedPath = installedPath.Replace("win32", "x64");
                    else if (!Environment.Is64BitProcess && installedPath.Contains("x64"))
                        installedPath = installedPath.Replace("x64", "win32");

                    if (File.Exists(Path.Combine(installedPath, BOA_DLL)))
                        return installedPath;
                }
            }
            catch { }

            var defaultDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                string.Format(BOA_PATH_FMT, Environment.Is64BitProcess ? "x64" : "win32"));
            if (File.Exists(Path.Combine(defaultDir, BOA_DLL)))
                return defaultDir;

            throw new Exception($"未检测到 ETAS BOA V2 驱动(注册表与默认路径均未找到 {BOA_DLL}),请先安装 ETAS BOA V2(随 INCA 等 ETAS 工具链发布)");
        }

        /// <summary>
        /// 首次使用时加载 dll-ocdProxy.dll / dll-csiBind.dll 并取出全部函数指针。
        /// 进程生命周期内不卸载(驱动 DLL 内部有全局状态)。
        /// </summary>
        internal static void EnsureLoaded()
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;

                string dir = ResolveBoaDirectory();

                // BOA DLL 的依赖项与其同目录,LoadLibrary 裸文件名时需要把该目录纳入搜索路径
                var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                if (!path.Contains(dir, StringComparison.OrdinalIgnoreCase))
                    Environment.SetEnvironmentVariable("PATH", path + ";" + dir);

                _boaModule = LoadLibrary(BOA_DLL);
                if (_boaModule == IntPtr.Zero)
                    throw new Exception($"加载 {BOA_DLL} 失败(目录: {dir})");
                _csiModule = LoadLibrary(CSI_DLL);
                if (_csiModule == IntPtr.Zero)
                    throw new Exception($"加载 {CSI_DLL} 失败(目录: {dir})");

                // OCI (dll-ocdProxy.dll)
                CreateCANControllerVersion = GetDelegate<OciCreateControllerVersion>(_boaModule, "OCI_CreateCANControllerVersion");
                DestroyCANController = GetDelegate<OciDestroyController>(_boaModule, "OCI_DestroyCANController");
                CloseCANController = GetDelegate<OciCloseController>(_boaModule, "OCI_CloseCANController");
                OpenCANController = GetDelegate<OciOpenCANController>(_boaModule, "OCI_OpenCANController");
                CreateCANTxQueue = GetDelegate<OciCreateCANTxQueue>(_boaModule, "OCI_CreateCANTxQueue");
                CreateCANRxQueue = GetDelegate<OciCreateCANRxQueue>(_boaModule, "OCI_CreateCANRxQueue");
                WriteCANDataEx = GetDelegate<OciWriteCANDataEx>(_boaModule, "OCI_WriteCANDataEx");
                AddCANFrameFilter = GetDelegate<OciAddCANFrameFilter>(_boaModule, "OCI_AddCANFrameFilter");
                GetTimerCapabilities = GetDelegate<OciGetTimerCapabilities>(_boaModule, "OCI_GetTimerCapabilities");

                // CSI (dll-csiBind.dll)
                CreateProtocolTree = GetDelegate<CsiCreateProtocolTree>(_csiModule, "CSI_CreateProtocolTree");
                DestroyProtocolTree = GetDelegate<CsiDestroyProtocolTree>(_csiModule, "CSI_DestroyProtocolTree");

                _initialized = true;
            }
        }

        private static T GetDelegate<T>(IntPtr module, string name) where T : Delegate
        {
            var ptr = GetProcAddress(module, name);
            if (ptr == IntPtr.Zero)
                throw new Exception($"BOA 函数 {name} 未找到");
            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }

        // ---------------- 错误码 ----------------

        /// <summary>
        /// BOA 错误码: 0x80000000=ERROR / 0x40000000=WARNING + 类别 + 序号
        /// </summary>
        [Flags]
        internal enum EtasErrorCode : uint
        {
            SUCCESS = 0,
            ERROR = 0x80000000,
            WARNING = 0x40000000,
            PARAM = 0x1000,
            BIND = 0x2000,
            SEMANTIC = 0x3000,
            RESOURCE = 0x4000,
            COMMUNICATION = 0x5000,
            INTERNAL = 0x6000,
            INVALID_PARAMETER = ERROR | PARAM | 0,
            INCONSISTENT_PARAMETER_SET = ERROR | PARAM | 1,
            INVALID_HANDLE = ERROR | PARAM | 2,
            BUFFER_OVERFLOW = ERROR | PARAM | 3,
            UNSUPPORTED_PARAMETER = ERROR | PARAM | 4,
            INVALID_FILTER = ERROR | PARAM | 5,
            FILTER_UNKNOWN = ERROR | PARAM | 6,
            INCONSISTENT_SELF_RECEPTION = WARNING | PARAM | 0,
            INVALID_STATE = ERROR | SEMANTIC | 0,
            NO_CONFIG = ERROR | SEMANTIC | 1,
            QUEUE_IS_FULL = ERROR | SEMANTIC | 2,
            OBJECT_IN_USE = ERROR | SEMANTIC | 3,
            LATER_VERSION_IN_USE = ERROR | SEMANTIC | 4,
            INCOMPATIBLE_CONFIG = ERROR | RESOURCE | 0,
            OUT_OF_MEMORY = ERROR | RESOURCE | 1,
            NO_RESOURCES = ERROR | RESOURCE | 2,
            TIMEOUT = ERROR | RESOURCE | 4,
            PARAM_ADAPTED = WARNING | RESOURCE | 0,
            NO_INORDER_TX = WARNING | RESOURCE | 1,
            PERFORMANCE_RISK = WARNING | RESOURCE | 2,
            DRIVER_NO_RESPONSE = ERROR | COMMUNICATION | 0,
            DRIVER_DISCONNECTED = ERROR | COMMUNICATION | 1,
            UNEXPECTED_NULL = ERROR | INTERNAL | 0,
            HW_NOT_READY = ERROR | INTERNAL | 1,
            OUT_OF_RANGE = ERROR | INTERNAL | 2,
            INTERNAL_STATE = ERROR | INTERNAL | 3,
            INTERNAL_HANDLE = ERROR | INTERNAL | 4,
            INTERNAL_OVERFLOW = ERROR | INTERNAL | 5,
            NOT_IMPLEMENTED = ERROR | INTERNAL | 6,
            SYSTEM_ERROR = ERROR | INTERNAL | 7,
            NO_DATA = ERROR | INTERNAL | 8,
            NOT_INITIALIZED = ERROR | INTERNAL | 9,
            PROTOCOL_VERSION_NOT_SUPPORTED = ERROR | BIND | 0,
            INVALID_ACCESS_SYNTAX = ERROR | BIND | 1,
            INVALID_ACCESS_PARAM = ERROR | BIND | 2,
            INVALID_TRANSFER_SYNTAX = ERROR | BIND | 3,
            NO_INTERFACE = ERROR | BIND | 4,
            HW_NOT_PRESENT = ERROR | BIND | 5,
            CANNOT_OPEN_DRIVER = ERROR | BIND | 6,
            LICENSE_MISSING = ERROR | BIND | 7,
            CANNOT_FIND_CHILDREN = ERROR | BIND | 8,
            NONUNIQUE_BIND_TARGET = ERROR | BIND | 9,
            WRONG_API_VERSION = ERROR | BIND | 10,
            UNKNOWN_SERVICE = ERROR | BIND | 11,
            OLDER_VERSION_IS_BETTER = WARNING | BIND | 0,
            API_V1_0_IS_UNAVAILABLE = WARNING | BIND | 1,
        }

        internal static bool IsSuccess(EtasErrorCode code) => code == EtasErrorCode.SUCCESS;

        /// <summary>
        /// 仅把带 ERROR 位(0x80000000)的返回值当失败, WARNING(如 PARAM_ADAPTED 参数被驱动微调)放行
        /// </summary>
        internal static bool IsError(EtasErrorCode code) => (code & EtasErrorCode.ERROR) != 0;

        // ---------------- 配置结构体 ----------------

        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasVersion
        {
            public byte major;
            public byte minor;
            public byte bugFix;
            public byte build;
        }

        /// <summary>OCI_ControllerProperties: 控制器运行模式(0=RUNNING 立即上线)</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasControllerProperties
        {
            public uint mode;
        }

        /// <summary>OCI_CAN FD 扩展配置(OCI_CANFDConfiguration, 40 字节)</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasCanFdConfiguration
        {
            public uint bitRate;                            // 数据段波特率(Hz)
            public uint samplePoint;                        // 采样点(单位 1%, 80 = 80%)
            public uint BTL_Cycles;                         // 每比特时间量子数
            public uint SJW;
            public uint flags;                              // OCI_CANFD_FLAGS
            public uint txSecondarySamplePointOffset;
            public uint canRxMode;                          // 2 = CAN_FRAMES_USING_CAN_MESSAGE
            public uint canFdRxMode;                        // 4 = CANFD_FRAMES_USING_CANFD_MESSAGE
            public uint canFdTxConfig;                      // 4 = USE_CAN_AND_CANFD_FRAMES
            public ushort txSecondarySamplePointFilterWindow;
            public ushort reserved;
        }

        /// <summary>OCI_CAN 控制器配置(OCI_CANConfiguration)</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasCanConfiguration
        {
            public uint baudrate;                           // 仲裁段波特率(Hz)
            public uint samplePoint;                        // 采样点(单位 1%, 75 = 75%)
            public uint samplesPerBit;                      // 1 = ONE_SAMPLE_PER_BIT
            public uint BTL_Cycles;
            public uint SJW;
            public uint syncEdge;                           // 1 = SINGLE_SYNC_EDGE
            public uint physicalMedia;                      // 1 = HIGH_SPEED
            public uint selfReceptionMode;                  // 0 = RECEPTION_OFF
            public uint busParticipationMode;               // 2 = ACTIVE
            public uint canFDEnabled;
            public EtasCanFdConfiguration canFDConfig;
            public uint canTxPolicy;                        // 1 = FIFO

            /// <summary>
            /// 构造控制器配置; fdBaudrateHz = 0 表示经典 CAN, 否则使能 CAN FD 并设置数据段波特率。
            /// 波特率/采样点/BTL_Cycles/SJW 均可自定义(OCI 直接下发,硬件不支持时返回 PARAM_ADAPTED/ERROR)
            /// </summary>
            internal static EtasCanConfiguration Create(uint baudrateHz, uint fdBaudrateHz,
                uint samplePoint = 75, uint btlCycles = 10, uint sjw = 1,
                uint fdSamplePoint = 80, uint fdBtlCycles = 10, uint fdSjw = 1)
            {
                return new EtasCanConfiguration
                {
                    baudrate = baudrateHz,
                    samplePoint = samplePoint,
                    samplesPerBit = 1,
                    BTL_Cycles = btlCycles,
                    SJW = sjw,
                    syncEdge = 1,
                    physicalMedia = 1,
                    selfReceptionMode = 0,
                    busParticipationMode = 2,
                    canFDEnabled = fdBaudrateHz == 0 ? 0u : 1u,
                    canFDConfig = new EtasCanFdConfiguration
                    {
                        bitRate = fdBaudrateHz,
                        samplePoint = fdSamplePoint,
                        BTL_Cycles = fdBtlCycles,
                        SJW = fdSjw,
                        flags = 0,
                        txSecondarySamplePointOffset = 0,
                        canRxMode = 2,
                        canFdRxMode = 4,
                        canFdTxConfig = 4,
                        txSecondarySamplePointFilterWindow = 0,
                        reserved = 0,
                    },
                    canTxPolicy = 1,
                };
            }
        }

        /// <summary>OCI_CANTxQueueConfiguration</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasTxQueueConfiguration
        {
            public uint reserved;
        }

        /// <summary>OCI_RxCallbackSingleMsg: 函数指针 + 用户数据</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasRxCallbackSingleMsg
        {
            public OciCallbackEx functionEx;
            public IntPtr userData;
        }

        /// <summary>OCI_CANRxQueueConfiguration</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasRxQueueConfiguration
        {
            public EtasRxCallbackSingleMsg onFrame;
            public EtasRxCallbackSingleMsg onEvent;
            public uint selfReceptionMode;  // 0 = RECEPTION_OFF
        }

        /// <summary>OCI_CANRxFilter: 全 0 即放行所有帧</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasRxFilter
        {
            public uint frameIDValue;
            public uint frameIDMask;
            public uint tag;
        }

        /// <summary>OCI_TimerCapabilities: 定时器能力; 本封装只用 tickFrequency(tick/秒) 换算报文时间戳, 其余字段保持布局占位</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct EtasTimerCapabilities
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string localClockID;
            public uint format;
            public uint tickFrequency;
            public uint ticksPerIncrement;
            public uint localStratumLevel;
            public uint localReferenceScale;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string localTimeOriginIso8601;
            public uint syncSlave;
            public uint syncMaster;
        }

        // ---------------- 消息结构体 ----------------

        // OCI_CAN 消息类型(回调消息头 type 字段)
        internal const uint MSG_TYPE_RX_MESSAGE = 1;
        internal const uint MSG_TYPE_TX_MESSAGE = 2;
        internal const uint MSG_TYPE_ERROR_FRAME = 3;
        internal const uint MSG_TYPE_BUS_EVENT = 4;
        internal const uint MSG_TYPE_INTERNAL_ERROR_EVENT = 5;
        internal const uint MSG_TYPE_QUEUE_EVENT = 6;
        internal const uint MSG_TYPE_TIMER_EVENT = 7;
        internal const uint MSG_TYPE_FDRX_MESSAGE = 8;
        internal const uint MSG_TYPE_FDTX_MESSAGE = 9;

        // OCI_CAN_MSG_FLAG 标志位(u16)
        internal const ushort MSG_FLAG_EXTENDED = 0x01;           // 扩展帧(29bit ID)
        internal const ushort MSG_FLAG_REMOTE_FRAME = 0x02;       // RTR 远程帧
        internal const ushort MSG_FLAG_SELFRECEPTION = 0x04;      // 自接收
        internal const ushort MSG_FLAG_FD_DATA_BIT_RATE = 0x08;   // CAN FD BRS
        internal const ushort MSG_FLAG_FD_TRUNC_AND_PAD = 0x10;   // FD 截断/填充
        internal const ushort MSG_FLAG_FD_ERROR_PASSIVE = 0x20;   // FD 发送节点错误被动(≈ESI)
        internal const ushort MSG_FLAG_FD_DATA = 0x40;            // CAN FD 帧

        /// <summary>OCI_CAN 发送消息(OCI_CANMessageEx 装 OCI_CANTxMessage, 24 字节), 与接收消息一样带 type+reserved 头</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasCanTxMsg
        {
            public uint type;       // 2 = OCI_CAN_TX_MESSAGE
            public uint reserved;
            public uint frameID;
            public ushort flags;
            public byte res;
            public byte dlc;        // 负载字节数(0..8), 非 4bit DLC 编码
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] data;
        }

        /// <summary>OCI_CANFD 发送消息(OCI_CANMessageEx 装 OCI_CANFDTxMessage, 80 字节)</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasCanFdTxMsg
        {
            public uint type;       // 9 = OCI_CANFDTX_MESSAGE
            public uint reserved;
            public uint frameID;
            public ushort flags;
            public byte res;
            public byte size;       // 负载字节数(0..64)
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] data;
        }

        /// <summary>OCI_CANRxMessage(40 字节), 回调消息带 type+reserved 头</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasCanRxMsg
        {
            public uint type;
            public uint reserved;
            public long timeStamp;  // OCI_Time 硬件 tick, 按 tickFrequency(见 QueryTickFrequency) 换算
            public uint tag;
            public uint frameID;
            public ushort flags;
            public byte res;
            public byte dlc;
            public uint res1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] data;
        }

        /// <summary>OCI_CANFDRxMessage(96 字节)</summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EtasCanFdRxMsg
        {
            public uint type;
            public uint reserved;
            public long timeStamp;  // OCI_Time 硬件 tick, 按 tickFrequency(见 QueryTickFrequency) 换算
            public uint tag;
            public uint frameID;
            public ushort flags;
            public byte res;
            public byte dlc;        // 官方名 size, 负载字节数(0..64)
            public uint res1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] data;
        }

        internal static readonly int TX_MSG_SIZE = Marshal.SizeOf<EtasCanTxMsg>();       // 24
        internal static readonly int FD_TX_MSG_SIZE = Marshal.SizeOf<EtasCanFdTxMsg>();  // 80

        // ---------------- CSI 硬件枚举结构体 ----------------

        // CSI 节点类型范围(物理节点)
        private const uint NODE_TYPE_MIN_PHYSICAL_NODE = 0x2000;
        private const uint NODE_TYPE_MAX_PHYSICAL_NODE = 0x2fff;

        [StructLayout(LayoutKind.Sequential)]
        private struct EtasNodeRange
        {
            public uint min;
            public uint max;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EtasUuidBin
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EtasUuidVersion
        {
            public EtasUuidBin uuid;
            public EtasVersion version;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EtasServiceId
        {
            public EtasUuidVersion api;
            public EtasUuidVersion access;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct EtasServiceIdParam
        {
            public EtasServiceId id;
            public uint count;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public byte[] accessParam;
        }

        /// <summary>CSI_SubItem(628 字节), 布局与 python-can 的 ctypes 绑定一致</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct EtasSubItem
        {
            public EtasServiceIdParam server;
            public uint nodeType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public byte[] uriName;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] visibleName;
            public EtasVersion version;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 88)]
            public byte[] reserved2;
            public EtasUuidBin serverAffinity;
            public EtasUuidBin requiredAffinity0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public int[] reserved;
            public EtasUuidBin requiredAffinity1;
            public uint count;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public EtasServiceId[] requiredAPI;

            public string Name => Encoding.ASCII.GetString(uriName).TrimEnd('\0');
        }

        /// <summary>CSI 协议树节点(非托管内存, Sibling/Child 为指针)</summary>
        [StructLayout(LayoutKind.Sequential)]
        private class EtasCsiTree
        {
            public EtasSubItem item;
            public IntPtr sibling;
            public IntPtr child;
            public int childrenProbed;
        }

        // ---------------- 函数委托 ----------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate EtasErrorCode OciCreateControllerVersion(string uriLocation, ref EtasVersion version, out int handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate EtasErrorCode OciDestroyController(int handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate EtasErrorCode OciCloseController(int handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate EtasErrorCode OciOpenCANController(int handle, ref EtasCanConfiguration configuration, ref EtasControllerProperties properties);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate EtasErrorCode OciCreateCANTxQueue(int handle, ref EtasTxQueueConfiguration configuration, out int queue);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate EtasErrorCode OciCreateCANRxQueue(int handle, ref EtasRxQueueConfiguration configuration, out int queue);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate EtasErrorCode OciWriteCANDataEx(int handle, long timeout, IntPtr[] messages, uint size, ref uint remaining);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate EtasErrorCode OciAddCANFrameFilter(int queue, EtasRxFilter[] filter, uint count);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate EtasErrorCode OciGetTimerCapabilities(int handle, ref EtasTimerCapabilities capabilities);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void OciCallbackEx(IntPtr userData, IntPtr msg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate EtasErrorCode CsiCreateProtocolTree(string uriName, EtasNodeRange range, ref IntPtr tree);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EtasErrorCode CsiDestroyProtocolTree(IntPtr tree);

        internal static OciCreateControllerVersion CreateCANControllerVersion = null!;
        internal static OciDestroyController DestroyCANController = null!;
        internal static OciCloseController CloseCANController = null!;
        internal static OciOpenCANController OpenCANController = null!;
        internal static OciCreateCANTxQueue CreateCANTxQueue = null!;
        internal static OciCreateCANRxQueue CreateCANRxQueue = null!;
        internal static OciWriteCANDataEx WriteCANDataEx = null!;
        internal static OciAddCANFrameFilter AddCANFrameFilter = null!;
        internal static OciGetTimerCapabilities GetTimerCapabilities = null!;
        private static CsiCreateProtocolTree CreateProtocolTree = null!;
        private static CsiDestroyProtocolTree DestroyProtocolTree = null!;

        /// <summary>
        /// 查询控制器硬件定时器频率(tick/秒), 用于把报文 timeStamp 换算成时间; 查询失败返回 0, 调用方按"无时间戳"降级
        /// </summary>
        internal static uint QueryTickFrequency(int ctrlHandle)
        {
            var caps = new EtasTimerCapabilities();
            return IsError(GetTimerCapabilities(ctrlHandle, ref caps)) ? 0 : caps.tickFrequency;
        }

        /// <summary>
        /// 通过 CSI 协议树枚举支持指定协议的硬件节点路径(如 "/ETAS.ETH://ES581.4/CAN:1")
        /// </summary>
        internal static List<string> EnumerateNodes(string protocol)
        {
            EnsureLoaded();

            IntPtr treeHandle = IntPtr.Zero;
            var range = new EtasNodeRange { min = NODE_TYPE_MIN_PHYSICAL_NODE, max = NODE_TYPE_MAX_PHYSICAL_NODE };
            var status = CreateProtocolTree(string.Empty, range, ref treeHandle);
            if (status != EtasErrorCode.SUCCESS || treeHandle == IntPtr.Zero)
                throw new Exception($"创建 CSI 协议树失败({status})");

            try
            {
                var nodes = new List<string>();
                // 按名称过滤(与 python-can 的 ETAS 绑定一致), nodeType 不作硬过滤, 避免驱动返回值差异导致漏枚举
                WalkTree(treeHandle, string.Empty, protocol, nodes, new HashSet<IntPtr>());
                nodes.Sort(StringComparer.Ordinal);
                return nodes;
            }
            finally
            {
                DestroyProtocolTree(treeHandle);
            }
        }

        // visited 防环: sibling/child 链若因驱动异常成环, 已访问节点直接截断, 不会死循环
        private static void WalkTree(IntPtr nodePtr, string prefix, string protocol, List<string> nodes, HashSet<IntPtr> visited)
        {
            while (nodePtr != IntPtr.Zero && visited.Add(nodePtr))
            {
                var node = Marshal.PtrToStructure<EtasCsiTree>(nodePtr);
                if (node == null) break;

                string name = prefix + "/" + node.item.Name;
                if (name.Contains(protocol))
                    nodes.Add(name);

                if (node.child != IntPtr.Zero)
                    WalkTree(node.child, name, protocol, nodes, visited);

                nodePtr = node.sibling;
            }
        }
    }
}
