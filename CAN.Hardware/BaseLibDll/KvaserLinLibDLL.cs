using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// Kvaser linlib 常量与状态码。
    /// </summary>
    public static class Linlib
    {
        public const int linINVALID_HANDLE = -1;

        /// <summary>本机发送(发送回显)</summary>
        public const int LIN_TX = 1;

        /// <summary>总线接收</summary>
        public const int LIN_RX = 2;

        /// <summary>唤醒帧</summary>
        public const int LIN_WAKEUP_FRAME = 4;

        /// <summary>无数据(仅收到报头,从机未应答)</summary>
        public const int LIN_NODATA = 8;

        /// <summary>校验和错误</summary>
        public const int LIN_CSUM_ERROR = 16;

        /// <summary>奇偶校验错误(ID 场校验位不符)</summary>
        public const int LIN_PARITY_ERROR = 32;

        /// <summary>同步场错误</summary>
        public const int LIN_SYNCH_ERROR = 64;

        /// <summary>位错误</summary>
        public const int LIN_BIT_ERROR = 128;

        public const int linCHANNELDATA_CARD_FIRMWARE_REV = 9;

        /// <summary>以主节点打开通道</summary>
        public const int LIN_MASTER = 1;

        /// <summary>以从节点打开通道</summary>
        public const int LIN_SLAVE = 2;

        // linSetupLIN 的 lFlags 组合位
        public const int LIN_MSG_DISTURB_CSUM = 1;
        public const int LIN_MSG_DISTURB_PARITY = 2;
        public const int LIN_MSG_USE_STANDARD_PARITY = 4;
        public const int LIN_MSG_USE_ENHANCED_PARITY = 8;

        /// <summary>增强校验和(LIN 2.x)</summary>
        public const int LIN_ENHANCED_CHECKSUM = 1;

        /// <summary>可变 DLC(帧长度由 ID 对应的表项决定)</summary>
        public const int LIN_VARIABLE_DLC = 2;

        /// <summary>
        /// linlib 状态码。负数为错误,0 为成功;
        /// 注意 linOpenChannel 失败时直接返回负数状态码而非句柄。
        /// </summary>
        public enum LinStatus
        {
            [Description("Succed")]
            linOK = 0,
            [Description("No messages available")]
            linERR_NOMSG = -1,
            [Description("Handle not on-bus")]
            linERR_NOTRUNNING = -3,
            [Description("Handle not off-bus")]
            linERR_RUNNING = -4,
            [Description("Only for a master")]
            linERR_MASTERONLY = -5,
            [Description("Only for a slave")]
            linERR_SLAVEONLY = -6,
            [Description("Error in parameter")]
            linERR_PARAM = -7,
            [Description("Specified hardware not found ,this error is reported when the LIN transceiver isn't powered up")]
            linERR_NOTFOUND = -8,
            [Description("Out of memory")]
            linERR_NOMEM = -9,
            [Description("No channels avaliable")]
            linERR_NOCHANNELS = -10,
            [Description("Timeout occurred")]
            linERR_TIMEOUT = -11,
            [Description("Library not initialized")]
            linERR_NOTINITIALIZED = -12,
            [Description("Can't get handle")]
            linERR_NOHANDLES = -13,
            [Description("Handle is invalid")]
            linERR_INVHANDLE = -14,
            [Description("Internal error in the driver")]
            linERR_CANERROR = -15,
            [Description("There was an error response from the LIN interface")]
            linERR_ERRRESP = -16,
            [Description("The LIN interface response wasn't the expected one")]
            linERR_WRONGRESP = -17,
            [Description("Can driver type not supported")]
            linERR_DRIVER = -18,
            [Description("DeviceIOControl failed,use the WIN32 GetLastError API to get the real error code")]
            linERR_DRIVERFAILED = -19,
            [Description("The card was removed or not inserted")]
            linERR_NOCARD = -20,
            [Description("The license is not valid")]
            linERR_LICENSE = -21,
            [Description("Internal error in the driver")]
            linERR_INTERNAL = -22,
            [Description("Access denied")]
            linERR_NO_ACCESS = -23,
            [Description("Function not supported in this version")]
            linERR_VERSION = -24,
            [Description("Function not supported is this version")]
            linERR_NO_REF_POWER = -25,
            [Description("The requested feature or function is not implemented in the device you are trying to use it on")]
            linERR_NOT_IMPLEMENTED = -26
        }
    }

    /// <summary>
    /// Kvaser LIN 库(linlib.dll)P/Invoke 封装。
    /// linlib 与 canlib32 是两个独立的库:LIN 通道需本库打开/操作,
    /// InitializeLibrary/UnloadLibrary 引用计数,多个 wrapper 实例共用一份库状态。
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class KvaserLinLibDLL
    {
        /// <summary>
        /// linReadMessage 返回的报文附加信息(Pack=1,共 64 字节)。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct LinMessageInfo
        {
            public uint timestamp;

            public uint synchBreakLength;

            public uint frameLength;

            public uint bitrate;

            public byte checkSum;

            public byte idPar;

            public ushort z;

            public unsafe fixed uint synchEdgeTime[4];

            public unsafe fixed uint byteTime[8];
        }

        private const string DLL_NAME = ".\\HwDependency\\Kvaser\\Generic\\Win\\x64\\linlib.dll";

        private static int _refCount = 0;
        private static readonly object _initLock = new object();

        [DllImport(DLL_NAME)]
        private static extern void linInitializeLibrary();

        [DllImport(DLL_NAME)]
        private static extern void linUnloadLibrary();

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linGetTransceiverData(int channel, IntPtr eanNo, IntPtr serNo, out int ttype);

        [DllImport(DLL_NAME)]
        public static extern int linOpenChannel(int channel, int flags);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linClose(int h);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linGetVersion(out int major, out int minor, out int build);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linGetFirmwareVersion(int h, out byte bootVerMajor, out byte bootVerMinor, out byte bootVerBuild, out byte appVerMajor, out byte appVerMinor, out byte appVerBuild);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linGetChannelData(int channel, int item, IntPtr buffer, ulong bufsize);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linSetBitrate(int h, uint bps);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linBusOn(int h);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linBusOff(int h);

        [DllImport(DLL_NAME)]
        public static extern uint linReadTimer(int h);

        /// <summary>主节点发送完整帧(报头+响应),仅主节点可用</summary>
        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linWriteMessage(int h, uint id, IntPtr msg, uint dlc);

        /// <summary>主节点发送报头并等待从机应答,仅主节点可用</summary>
        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linRequestMessage(int h, uint id);

        /// <summary>非阻塞读取一帧,无消息时返回 linERR_NOMSG</summary>
        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linReadMessage(int h, out uint id, IntPtr msg, out uint dlc, out uint flags, ref LinMessageInfo msgInfo);

        /// <summary>限时等待读取一帧,超时返回 linERR_TIMEOUT</summary>
        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linReadMessageWait(int h, out uint id, IntPtr msg, out uint dlc, out uint flags, ref LinMessageInfo msgInfo, uint timeout);

        /// <summary>从节点更新本节点某 ID 的应答数据,收到匹配报头时发出,仅从节点可用</summary>
        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linUpdateMessage(int h, uint id, IntPtr msg, uint dlc);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linSetupIllegalMessage(int h, uint id, uint cFlags, uint delay);

        /// <summary>设置 LIN 特性(增强校验和/可变 DLC)与波特率</summary>
        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linSetupLIN(int h, uint lFlags, uint bps);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linWriteWakeup(int h, uint count, uint interval);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linClearMessage(int h, uint id);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linWriteSync(int h, uint timeout);

        [DllImport(DLL_NAME)]
        public static extern Linlib.LinStatus linGetCanHandle(int h, out uint canHandle);

        /// <summary>
        /// 初始化 linlib(引用计数,整个进程只执行一次真正的初始化)
        /// </summary>
        public static void InitializeLibrary()
        {
            lock (_initLock)
            {
                if (_refCount == 0)
                {
                    linInitializeLibrary();
                }
                _refCount++;
            }
        }

        /// <summary>
        /// 卸载 linlib(引用计数,归零才真正卸载)
        /// </summary>
        public static void UnloadLibrary()
        {
            lock (_initLock)
            {
                if (_refCount > 0)
                {
                    _refCount--;
                    if (_refCount == 0)
                    {
                        linUnloadLibrary();
                    }
                }
            }
        }
    }
}
