using System.Runtime.InteropServices;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// ESD(ntcan) 互操作层：运行时动态加载 ntcan.dll(32位)/ntcan64.dll(64位)，
    /// 经 LoadLibrary + GetProcAddress 解析导出函数为委托(全部 Cdecl)。
    /// 依赖 ESD CAN 驱动(NTCAN API,随 esd electronics CAN 接口卡发布)，未安装时 <see cref="EnsureLoaded"/> 抛出带指导信息的异常。
    /// 进程内加载后不主动卸载(委托生命周期与模块句柄绑定)。
    /// </summary>
    internal static class EsdNtcanLibDLL
    {
        #region 常量与枚举

        /// <summary>ntcan 原生扩展帧标记(bit29),与本工程托管层 SocketCAN 风格 bit31(CanFrameDef.CAN_EFF_FLAG)不同,需双向换乘</summary>
        internal const uint NTCAN_EXT_FLAG = 0x20000000;
        internal const uint NTCAN_EXT_ID_MASK = 0x1FFFFFFF;
        internal const uint NTCAN_STD_ID_MASK = 0x7FF;

        // BaseMsg.Len 字节位定义: bit0-3=DLC, bit4=RTR(经典)/NO_BRS(FD), bit5=NO_DATA, bit7=FD 帧
        internal const byte LEN_MASK_DLC = 0x0F;
        internal const byte LEN_FLAG_RTR = 0x10;
        internal const byte LEN_FLAG_FD_NO_BRS = 0x10;
        internal const byte LEN_FLAG_NO_DATA = 0x20;
        internal const byte LEN_FLAG_FD = 0x80;

        /// <summary>canSetBaudrateX 的 Mode 取值: 直接按数值波特率配置</summary>
        internal const ushort BAUDRATE_MODE_NUM = 4;
        /// <summary>canSetBaudrateX 的 Flags 取值: 使能 CAN FD</summary>
        internal const ushort BAUDRATE_FLAG_FD = 1;

        /// <summary>canOpen 句柄标志</summary>
        [Flags]
        internal enum EsdCanMode : uint
        {
            NONE = 0,
            NO_RTR = 0x00000010,           // 丢弃收到的 RTR 帧
            NO_DATA = 0x00000020,          // 丢弃收到的数据帧
            NO_INTERACTION = 0x00000100,
            MARK_INTERACTION = 0x00000200,
            LOCAL_ECHO = 0x00000400,       // 本地回声
            TIMESTAMPED_TX = 0x00020000,   // 带时间戳发送
            FD = 0x00040000,               // 使能 CAN FD
            OBJECT = 0x10000000,           // 对象模式(接收)
            OVERLAPPED = 0x20000000,       // 异步/重叠 I/O
        }

        /// <summary>canSetBaudrate 参数模式(高 4 位为模式标志,低 28 位为参数)</summary>
        internal enum EsdBaudMode : uint
        {
            LISTEN_ONLY_MODE = 0x40000000,
            USER_BAUDRATE_NUM = 0x20000000,   // 低 28 位为数值波特率(bps)
            USER_BAUDRATE = 0x80000000,       // 低 16 位为 BTR 寄存器值
            SELF_TEST_MODE = 0x10000000,
            AUTOBAUD = 0x00FFFFFE,
            NO_BAUDRATE = 0x7FFFFFFF,
        }

        /// <summary>ntcan 返回码, 0=OK, 0xE0000000 起为错误区</summary>
        internal enum EsdCanResult : uint
        {
            OK = 0,
            NEED_FW_UPDATE = 1,
            HW_ERROR = 2,
            ERRNO_BASE = 0xE0000000,
            RX_TIMEOUT = ERRNO_BASE + 1,
            TX_TIMEOUT = ERRNO_BASE + 2,
            TX_ERROR = ERRNO_BASE + 4,
            CONTR_OFF_BUS = ERRNO_BASE + 5,
            CONTR_BUSY = ERRNO_BASE + 6,
            CONTR_WARN = ERRNO_BASE + 7,
            OLDDATA = ERRNO_BASE + 8,
            NO_ID_ENABLED = ERRNO_BASE + 9,
            ID_ALREADY_ENABLED = ERRNO_BASE + 10,
            ID_NOT_ENABLED = ERRNO_BASE + 11,
            INVALID_FIRMWARE = ERRNO_BASE + 13,
            MESSAGE_LOST = ERRNO_BASE + 14,
            INVALID_HARDWARE = ERRNO_BASE + 15,
            PENDING_WRITE = ERRNO_BASE + 16,
            PENDING_READ = ERRNO_BASE + 17,
            INVALID_DRIVER = ERRNO_BASE + 18,
            WRONG_DEVICE_STATE = ERRNO_BASE + 19,
            HANDLE_FORCED_CLOSE = ERRNO_BASE + 20,
            NOT_SUPPORTED = ERRNO_BASE + 21,
            CONTR_ERR_PASSIVE = ERRNO_BASE + 22,
            ERROR_NO_BAUDRATE = ERRNO_BASE + 23,
            ERROR_LOM = ERRNO_BASE + 24,
            NO_CAN_CAPABILITY = ERRNO_BASE + 25,
            NO_LIN_CAPABILITY = ERRNO_BASE + 26,
            SOCK_CONN_TIMEOUT = ERRNO_BASE + 0x80,
            SOCK_CMD_TIMEOUT = ERRNO_BASE + 0x81,
            SOCK_HOST_NOT_FOUND = ERRNO_BASE + 0x82,
        }

        #endregion

        #region 结构体(全部 Pack=1)

        /// <summary>消息公共头(8 字节): ntcan CMSG/CMSGF 头部</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct EsdBaseMsg
        {
            public uint Id;        // 11/29 位 CAN ID, 扩展帧用 bit29(NTCAN_EXT_FLAG)标记
            public byte Len;       // bit0-3=DLC, bit4=RTR/NO_BRS, bit5=NO_DATA, bit7=FD
            public byte MsgLost;   // Rx 丢帧计数
            public byte Reserved;
            public byte Esi;       // Error State Indicator(仅 CAN FD 有效)
        }

        /// <summary>经典 CAN 消息(24 字节): 8 字节数据打包为小端 ulong</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct EsdCanMsg
        {
            public EsdBaseMsg Base;
            public ulong Data;       // 8 字节 CAN 数据(小端, data[0] 在最低字节)
            public ulong Timestamp;  // 时间戳(微秒)
        }

        /// <summary>CAN FD 消息(80 字节): 64 字节数据; 接收前必须预分配 Data 数组</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct EsdCanFdMsg
        {
            public EsdBaseMsg Base;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] Data;
            public ulong Timestamp;  // 时间戳(微秒)
        }

        /// <summary>TDC(收发延迟补偿)配置, 本封装保持默认全 0(关闭)</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct EsdTdcCfg
        {
            public byte TdcMode;
            public byte SspOffset;
            public sbyte SspShift;
            public byte TdcFilter;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct EsdBaudrateCfgNum
        {
            public uint Value;       // 数值波特率(bps)
            public uint Reserved;
        }

        /// <summary>canSetBaudrateX 的数值模式参数(24 字节): 仲裁段 + 数据段直接填 bps</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct EsdBaudrateXNum
        {
            public ushort Mode;      // BAUDRATE_MODE_NUM
            public ushort Flags;     // BAUDRATE_FLAG_FD
            public EsdTdcCfg Tdc;
            public EsdBaudrateCfgNum Arb;
            public EsdBaudrateCfgNum Data;
        }

        #endregion

        #region 委托声明(全部 Cdecl)

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EsdCanResult OpenFunc(int channel, EsdCanMode flags, int txQueueSize, int rxQueueSize, int txTimeout, int rxTimeout, out IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EsdCanResult CloseFunc(IntPtr handle);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EsdCanResult ReadTFunc(IntPtr handle, ref EsdCanMsg msg, ref int len, IntPtr overlapped);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EsdCanResult WriteTFunc(IntPtr handle, ref EsdCanMsg msg, ref int len, IntPtr overlapped);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EsdCanResult ReadXFunc(IntPtr handle, ref EsdCanFdMsg msg, ref int len, IntPtr overlapped);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EsdCanResult WriteXFunc(IntPtr handle, ref EsdCanFdMsg msg, ref int len, IntPtr overlapped);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EsdCanResult SetBaudrateFunc(IntPtr handle, uint baud);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EsdCanResult SetBaudrateXFunc(IntPtr handle, EsdBaudrateXNum baudrateConfig);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate EsdCanResult IdRegionAddFunc(IntPtr handle, int idStart, ref int idCount);

        #endregion

        #region 动态加载

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private static readonly object _loadLock = new object();
        private static IntPtr _hModule = IntPtr.Zero;

        private static OpenFunc? _open;
        private static CloseFunc? _close;
        private static ReadTFunc? _readT;
        private static WriteTFunc? _writeT;
        private static ReadXFunc? _readX;
        private static WriteXFunc? _writeX;
        private static SetBaudrateFunc? _setBaudrate;
        private static SetBaudrateXFunc? _setBaudrateX;
        private static IdRegionAddFunc? _idRegionAdd;

        /// <summary>定位并加载 ntcan.dll/ntcan64.dll, 解析全部导出函数; 重复调用为空操作</summary>
        internal static void EnsureLoaded()
        {
            if (_hModule != IntPtr.Zero) return;
            lock (_loadLock)
            {
                if (_hModule != IntPtr.Zero) return;

                string dllName = Environment.Is64BitProcess ? "ntcan64.dll" : "ntcan.dll";
                IntPtr hModule = LoadLibrary(dllName);
                if (hModule == IntPtr.Zero)
                    throw new DllNotFoundException($"未检测到 ESD NTCAN 驱动(未找到 {dllName}),请先安装 ESD CAN 驱动(NTCAN API,随 esd electronics CAN 接口卡发布)");

                _open = GetDelegate<OpenFunc>(hModule, "canOpen");
                _close = GetDelegate<CloseFunc>(hModule, "canClose");
                _readT = GetDelegate<ReadTFunc>(hModule, "canReadT");
                _writeT = GetDelegate<WriteTFunc>(hModule, "canWriteT");
                _readX = GetDelegate<ReadXFunc>(hModule, "canReadX");
                _writeX = GetDelegate<WriteXFunc>(hModule, "canWriteX");
                _setBaudrate = GetDelegate<SetBaudrateFunc>(hModule, "canSetBaudrate");
                _setBaudrateX = GetDelegate<SetBaudrateXFunc>(hModule, "canSetBaudrateX");
                _idRegionAdd = GetDelegate<IdRegionAddFunc>(hModule, "canIdRegionAdd");

                // 关键函数缺失时立即报错,避免后续空调用 NullReferenceException(参考实现此处静默返回 null)
                if (_open == null || _close == null || _readT == null || _writeT == null
                    || _readX == null || _writeX == null || _setBaudrate == null
                    || _setBaudrateX == null || _idRegionAdd == null)
                {
                    throw new EntryPointNotFoundException($"{dllName} 导出函数不完整(canOpen/canClose/canReadT/canWriteT/canReadX/canWriteX/canSetBaudrate/canSetBaudrateX/canIdRegionAdd),请确认安装的是完整的 ESD NTCAN 驱动");
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

        internal static EsdCanResult CanOpen(int channel, EsdCanMode flags, int txQueueSize, int rxQueueSize, int txTimeout, int rxTimeout, out IntPtr handle)
        {
            EnsureLoaded();
            return _open!(channel, flags, txQueueSize, rxQueueSize, txTimeout, rxTimeout, out handle);
        }

        internal static EsdCanResult CanClose(IntPtr handle)
        {
            EnsureLoaded();
            return _close!(handle);
        }

        /// <summary>canReadT: 阻塞读取经典 CAN 消息(超时时间由 canOpen 的 rxTimeout 决定), len 入参为缓冲条数(1),出参为实际读到条数</summary>
        internal static EsdCanResult CanRead(IntPtr handle, ref EsdCanMsg msg, ref int len)
        {
            EnsureLoaded();
            return _readT!(handle, ref msg, ref len, IntPtr.Zero);
        }

        internal static EsdCanResult CanWrite(IntPtr handle, ref EsdCanMsg msg, ref int len)
        {
            EnsureLoaded();
            return _writeT!(handle, ref msg, ref len, IntPtr.Zero);
        }

        /// <summary>canReadX: 阻塞读取 FD 通道消息(经典帧与 FD 帧均可), msg.Data 必须预分配 64 字节</summary>
        internal static EsdCanResult CanReadX(IntPtr handle, ref EsdCanFdMsg msg, ref int len)
        {
            EnsureLoaded();
            return _readX!(handle, ref msg, ref len, IntPtr.Zero);
        }

        /// <summary>canWriteX: FD 通道发送(经典帧不带 LEN_FLAG_FD 位, FD 帧带 FD/BRS 位)</summary>
        internal static EsdCanResult CanWriteX(IntPtr handle, ref EsdCanFdMsg msg, ref int len)
        {
            EnsureLoaded();
            return _writeX!(handle, ref msg, ref len, IntPtr.Zero);
        }

        internal static EsdCanResult CanSetBaudrate(IntPtr handle, uint baud)
        {
            EnsureLoaded();
            return _setBaudrate!(handle, baud);
        }

        internal static EsdCanResult CanSetBaudrateX(IntPtr handle, EsdBaudrateXNum config)
        {
            EnsureLoaded();
            return _setBaudrateX!(handle, config);
        }

        /// <summary>canIdRegionAdd: 使能一段 ID 接收范围(ntcan 默认关闭所有 ID,需显式使能), idCount 为 in/out(驱动可回写实际使能数量)</summary>
        internal static EsdCanResult CanIdRegionAdd(IntPtr handle, int idStart, ref int idCount)
        {
            EnsureLoaded();
            return _idRegionAdd!(handle, idStart, ref idCount);
        }

        #endregion
    }
}
