using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// Vector 硬件类型(XL HWTYPE_xxx,参考 Vector vxlapi.h)。
    /// </summary>
    public enum VectorHwType : uint
    {
        NONE = 0,
        Virtual = 1,
        CANcardX = 2,
        [Description("CAN-AC2-PCI")]
        CANAC2PCI = 6,
        CANcardY = 12,
        CANcardXL = 15,
        CANcaseXL = 21,
        [Description("CANboardXL, CANboardXL PCIe")]
        CANBOARDXL = 25,
        [Description("CANboardXL pxi")]
        CANBOARDXL_PXI = 27,
        VN2600 = 29,
        VN2610 = VN2600,
        VN3300 = 37,
        VN3600 = 39,
        VN7600 = 41,
        CANcardXLe = 43,
        VN8900 = 45,
        VN8950 = 47,
        VN2640 = 53,
        VN1610 = 55,
        VN1630 = 57,
        VN1640 = 59,
        VN8970 = 61,
        VN1611 = 63,
        VN5240,
        VN5610,
        VN5620,
        VN7570,
        VN5650,
        IPCLIENT,
        VX1121 = 73,
        VX1131 = 75,
        VT6204 = 77,
        VN1630_LOG = 79,
        VN7610 = 81,
        VN7572 = 83,
        VN8972 = 85,
        VN0601 = 87,
        VN5640 = 89,
        VX0312 = 91,
        VN8800 = 95,
        IPCL8800,
        IPSRV8800,
        CSMCAN,
        VN5610A = 101,
        VN7640,
        VX1135 = 104,
        VN4610,
        VT6306 = 107,
        VT6104A,
        VN5430,
        VTSSERVICE,
        VN1530 = 112,
        VN1531,
        VX1161A,
        VX1161B,
    }

    /// <summary>
    /// Vector XL API 返回状态码(XLstatus)。
    /// </summary>
    public enum XLstatus : short
    {
        [Description("成功")]
        SUCCESS = 0,
        PENDING = 1,
        [Description("接收队列为空")]
        ERR_QUEUE_IS_EMPTY = 10,
        [Description("发送队列已满")]
        ERR_QUEUE_IS_FULL = 11,
        ERR_TX_NOT_POSSIBLE = 12,
        ERR_NO_LICENSE = 14,
        ERR_WRONG_PARAMETER = 101,
        ERR_TWICE_REGISTER = 110,
        ERR_INVALID_CHAN_INDEX = 111,
        ERR_INVALID_ACCESS = 112,
        ERR_PORT_IS_OFFLINE = 113,
        ERR_CHAN_IS_ONLINE = 116,
        ERR_NOT_IMPLEMENTED = 117,
        ERR_INVALID_PORT = 118,
        ERR_HW_NOT_READY = 120,
        ERR_CMD_TIMEOUT = 121,
        ERR_CMD_HANDLING = 122,
        ERR_HW_NOT_PRESENT = 129,
        ERR_NOTIFY_ALREADY_ACTIVE = 131,
        ERR_INVALID_TAG = 132,
        ERR_INVALID_RESERVED_FLD = 133,
        ERR_INVALID_SIZE = 134,
        ERR_INSUFFICIENT_BUFFER = 135,
        ERR_ERROR_CRC = 136,
        ERR_BAD_EXE_FORMAT = 137,
        ERR_NO_SYSTEM_RESOURCES = 138,
        ERR_NOT_FOUND = 139,
        ERR_INVALID_ADDRESS = 140,
        ERR_REQ_NOT_ACCEPT = 141,
        ERR_INVALID_LEVEL = 142,
        ERR_NO_DATA_DETECTED = 143,
        ERR_INTERNAL_ERROR = 144,
        ERR_UNEXP_NET_ERR = 145,
        ERR_INVALID_USER_BUFFER = 146,
        ERR_NO_RESOURCES = 152,
        ERR_WRONG_CHIP_TYPE = 153,
        ERR_WRONG_COMMAND = 154,
        ERR_INVALID_HANDLE = 155,
        ERR_RESERVED_NOT_ZERO = 157,
        [Description("缺少初始化权限,请先在Vector Hardware Config中为该应用分配通道")]
        ERR_INIT_ACCESS_MISSING = 158,
        [Description("无法打开驱动,请确认已安装Vector XL Driver")]
        ERR_CANNOT_OPEN_DRIVER = 201,
        ERR_WRONG_BUS_TYPE = 202,
        [Description("未找到vxlapi64.dll,请安装Vector XL Driver Library")]
        ERR_DLL_NOT_FOUND = 203,
        ERR_INVALID_CHANNEL_MASK = 204,
        ERR_NOT_SUPPORTED = 205,
        ERR_CONNECTION_BROKEN = 210,
        ERR_CONNECTION_CLOSED = 211,
        ERR_INVALID_STREAM_NAME = 212,
        ERR_CONNECTION_FAILED = 213,
        ERR_STREAM_NOT_FOUND = 214,
        ERR_STREAM_NOT_CONNECTED = 215,
        ERR_QUEUE_OVERRUN = 216,
        ERROR = 255,
        ERR_INVALID_DLC = 0x0201,
        ERR_INVALID_CANID = 0x0202,
        ERR_INVALID_FDFLAG_MODE20 = 0x0203,
        ERR_EDL_RTR = 0x0204,
        ERR_EDL_NOT_SET = 0x0205,
        ERR_UNKNOWN_FLAG = 0x0206,
    }

    /// <summary>XL 总线类型。</summary>
    public enum XLbusType : uint
    {
        NONE = 0,
        CAN = 1,
        LIN = 2,
        FLEXRAY = 4,
        AFDX = 8,
        MOST = 16,
        DAIO = 64,
        J1708 = 128,
        KLINE = 1024,
        ETHERNET = 2048,
        A429 = 4096,
    }

    /// <summary>XL 接口版本。</summary>
    public enum XLinterfaceVersion : uint { V3 = 3, V4 = 4 }

    /// <summary>通道激活标志。</summary>
    [Flags]
    public enum XLactivateFlags : uint { NONE = 0, RESET_CLOCK = 8 }

    /// <summary>经典 CAN 事件标签(XLevent.tag)。</summary>
    public enum XLeventTag : byte
    {
        XL_NO_COMMAND = 0,
        XL_RECEIVE_MSG = 1,
        XL_CHIP_STATE = 4,
        XL_TRANSCEIVER_INFO = 6,
        XL_TIMER = 8,
        XL_TRANSMIT_MSG = 10,
        XL_SYNC_PULSE = 11,
        XL_APPLICATION_NOTIFICATION = 15,
    }

    /// <summary>CAN FD 事件标签(XLcanRxEvent/XLcanTxEvent.tag)。</summary>
    public enum XLcanFdEventTag : ushort
    {
        RX_OK = 0x0400,
        RX_ERROR = 0x0401,
        TX_ERROR = 0x0402,
        TX_REQUEST = 0x0403,
        TX_OK = 0x0404,
        CHIP_STATE = 0x0409,
        TX_MSG = 0x0440,
    }

    /// <summary>经典 CAN 报文标志(XLevent.msgFlags)。</summary>
    [Flags]
    public enum XLcanMsgFlags : ushort
    {
        NONE = 0,
        ERROR_FRAME = 0x01,
        OVERRUN = 0x02,
        NERR = 0x04,
        WAKEUP = 0x08,
        REMOTE_FRAME = 0x10,
        TX_COMPLETED = 0x40,
        TX_REQUEST = 0x80,
        SRR_BIT_DOM = 0x200,
    }

    /// <summary>CAN FD 发送标志(XLcanTxEvent.flags)。</summary>
    [Flags]
    public enum XLcanTxMsgFlags : uint
    {
        NONE = 0,
        EDL = 0x01,
        BRS = 0x02,
        RTR = 0x10,
        HIGHPRIO = 0x80,
        WAKEUP = 0x200,
    }

    /// <summary>CAN FD 接收标志(XLcanRxEvent.msgFlags)。</summary>
    [Flags]
    public enum XLcanFdRxFlags : uint
    {
        EDL = 0x01,
        BRS = 0x02,
        ESI = 0x04,
        RTR = 0x10,
        EF = 0x0200,
        ARB_LOST = 0x0400,
        WAKEUP = 0x2000,
        TE = 0x4000,
    }

    /// <summary>经典 CAN 自定义波特率参数(XLchipParams)。</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XLchipParams
    {
        public uint bitRate;
        public byte sjw;
        public byte tseg1;
        public byte tseg2;
        public byte sam;        // 1 或 3
        public byte outputMode;
    }

    /// <summary>经典 CAN 事件(XLevent,tag = XL_RECEIVE_MSG / XL_TRANSMIT_MSG)。</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XLevent
    {
        public byte tag;
        public byte channelIndex;
        public ushort transId;
        public ushort portHandle;
        public byte flags;
        public byte reserved0;
        public ulong timeStamp;     // 单位:ns
        public uint id;             // bit31 = 扩展帧标志
        public ushort msgFlags;
        public ushort dlc;
        public ulong reserved1;
        public ulong data;          // 8字节数据,小端
        public ulong reserved2;
    }

    /// <summary>CAN FD 配置(XLcanFdConf)。</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XLcanFdConf
    {
        public uint arbitrationBitRate;
        public uint sjwAbr;
        public uint tseg1Abr;
        public uint tseg2Abr;
        public uint dataBitRate;
        public uint sjwDbr;
        public uint tseg1Dbr;
        public uint tseg2Dbr;
        public byte reserved;
        public byte options;        // 0x08 = CANFD-BOSCH(non-ISO)
        public ushort reserved1;
        public uint reserved2;
    }

    /// <summary>CAN FD 接收事件(XLcanRxEvent)。</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XLcanRxEvent
    {
        public uint size;           // 调用前须置为结构体大小
        public ushort tag;
        public ushort channelIndex;
        public uint userHandle;
        public ushort flagsChip;
        public ushort reserved0;
        public ulong reserved1;
        public ulong timeStamp;     // 单位:ns
        public uint canId;          // bit31 = 扩展帧标志
        public uint msgFlags;
        public uint crc;
        public ulong reserved2;
        public uint reserved3;
        public ushort totalBitCnt;
        public byte dlc;
        public byte reserved4;
        public uint reserved5;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] data;
    }

    /// <summary>CAN FD 发送事件(XLcanTxEvent)。</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XLcanTxEvent
    {
        public ushort tag;          // 0x0440 = XL_CAN_EV_TAG_TX_MSG
        public ushort transId;      // 0xFFFF
        public byte channelIndex;
        public byte reserved0;
        public byte reserved1;
        public byte reserved2;
        public uint id;             // bit31 = 扩展帧标志
        public uint flags;          // XLcanTxMsgFlags
        public byte dlc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] reserved3;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] data;
    }

    /// <summary>XL 通道配置(XLchannelConfig)。</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XLchannelConfig
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] name;
        public byte hwType;
        public byte hwIndex;
        public byte hwChannel;
        public ushort transceiverType;
        public ushort transceiverState;
        public ushort configError;
        public byte channelIndex;
        public ulong channelMask;
        public uint channelCapabilities;
        public uint channelBusCapabilities;
        public byte isOnBus;
        public uint connectedBusType;
        public XLbusType busType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] busParams;
        public uint doNotUse;
        public uint driverVersion;
        public uint interfaceVersion;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public uint[] raw_data;
        public uint serialNumber;
        public uint articleNumber;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] transceiverName;
        public uint specialCabFlags;
        public uint dominantTimeout;
        public byte dominantRecessiveDelay;
        public byte recessiveDominantDelay;
        public byte connectionInfo;
        public byte currentlyAvailableTimestamps;
        public ushort minimalSupplyVoltage;
        public ushort maximalSupplyVoltage;
        public uint maximalBaudrate;
        public byte fpgaCoreCapabilities;
        public byte specialDeviceStatus;
        public ushort channelBusActiveCapabilities;
        public ushort breakOffset;
        public ushort delimiterOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] reserved;
    }

    /// <summary>XL 驱动配置(XLdriverConfig)。</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XLdriverConfig
    {
        public uint dllVersion;
        public uint channelCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public uint[] reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public XLchannelConfig[] channel;
    }

    /// <summary>XL 通道信息(诊断/通道选择用)。</summary>
    public class XLchannelInfo
    {
        public string Name { get; set; } = string.Empty;
        public byte HwType { get; set; }
        public byte HwIndex { get; set; }
        public uint HwChannel { get; set; }
        public byte ChannelIndex { get; set; }
        public ulong ChannelMask { get; set; }
        public XLbusType BusType { get; set; }
        public bool IsOnBus { get; set; }

        public override string ToString() => Name ?? $"hwChannel {HwChannel}";
    }

    /// <summary>经典 CAN 总线参数读回结果(诊断/校验用)。</summary>
    public class XLcanBusParamsInfo
    {
        public string ChannelName = string.Empty;
        public uint BitRate;
        public byte Sjw;
        public byte Tseg1;
        public byte Tseg2;
        public byte Sam;
        public byte OutputMode;
        public byte CanOpMode;
    }

    /// <summary>
    /// Vector XL Driver Library(vxlapi64.dll)封装。
    /// 依赖随工程携带的 vxlapi64.dll(仅支持64位进程,与本工程其它硬件依赖一致)。
    /// </summary>
    public static class VectorCanLibDLL
    {
        private const string VcCANDLL_NAME = ".\\HwDependency\\Vector\\Generic\\Win\\x64\\vxlapi64.dll";
        /// <summary>扩展帧ID标志位(XL_CAN_EXT_MSG_ID)。</summary>
        public const uint XL_CAN_EXT_MSG_ID = 0x80000000;
        /// <summary>ID有效位掩码。</summary>
        public const uint XL_CAN_ID_MASK = 0x1FFFFFFF;
        /// <summary>CAN控制器输出模式:正常收发(XL_OUTPUT_MODE_NORMAL)。</summary>
        public const byte XL_OUTPUT_MODE_NORMAL = 1;

        [DllImport(VcCANDLL_NAME)]
        private static extern XLstatus xlOpenDriver();

        [DllImport(VcCANDLL_NAME)]
        private static extern XLstatus xlCloseDriver();

        [DllImport(VcCANDLL_NAME)]
        private static extern XLstatus xlGetDriverConfig(ref XLdriverConfig pDriverConfig);

        [DllImport(VcCANDLL_NAME, CharSet = CharSet.Ansi)]
        public static extern XLstatus xlSetApplConfig(string appName, uint appChannel, ulong hwType, uint hwIndex, uint hwChannel, XLbusType busType);

        [DllImport(VcCANDLL_NAME, CharSet = CharSet.Ansi)]
        public static extern XLstatus xlOpenPort(ref int portHandle, string userName, ulong accessMask, ref ulong permissionMask, uint rxQueueSize, XLinterfaceVersion xlInterfaceVersion, XLbusType busType);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlClosePort(int portHandle);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlActivateChannel(int portHandle, ulong accessMask, XLbusType busType, XLactivateFlags flags);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlDeactivateChannel(int portHandle, ulong accessMask);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanSetChannelBitrate(int portHandle, ulong accessMask, uint bitrate);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanSetChannelParams(int portHandle, ulong accessMask, ref XLchipParams pChipParams);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanSetAcceptanceFilter(int portHandle, ulong accessMask, uint code, uint mask, uint idRange);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanSetChannelMode(int portHandle, ulong accessMask, int tx, int txrq);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanFdSetConfiguration(int portHandle, ulong accessMask, ref XLcanFdConf pCanFdConf);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlReceive(int portHandle, ref uint pEventCount, ref XLevent pEvents);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanReceive(int portHandle, ref XLcanRxEvent pXlCanRxEvent);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanTransmit(int portHandle, ulong accessMask, ref uint pMessageCount, ref XLevent pMessages);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanTransmitEx(int portHandle, ulong accessMask, uint msgCnt, ref uint pMsgCntSent, XLcanTxEvent[] pMsgCnt);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlFlushReceiveQueue(int portHandle);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanFlushTransmitQueue(int portHandle, ulong accessMask);

        [DllImport(VcCANDLL_NAME)]
        public static extern XLstatus xlCanRequestChipState(int portHandle, ulong accessMask);

        private static int _refCount = 0;
        private static readonly object _initLock = new object();

        /// <summary>
        /// 打开 Vector 驱动(引用计数,多个Wrapper实例共享,仅首个实例真正打开)。
        /// </summary>
        public static XLstatus OpenDriver()
        {
            lock (_initLock)
            {
                XLstatus status = XLstatus.SUCCESS;
                if (_refCount == 0) status = xlOpenDriver();
                if (status == XLstatus.SUCCESS) _refCount++;
                return status;
            }
        }

        /// <summary>
        /// 关闭 Vector 驱动(引用计数归零时真正关闭)。
        /// </summary>
        public static void CloseDriver()
        {
            lock (_initLock)
            {
                if (_refCount <= 0) return;
                _refCount--;
                if (_refCount == 0) xlCloseDriver();
            }
        }

        /// <summary>读取驱动配置(调用前须已 OpenDriver)。</summary>
        private static bool TryGetDriverConfig(out XLdriverConfig driverConfig)
        {
            driverConfig = new XLdriverConfig
            {
                reserved = new uint[10],
                channel = new XLchannelConfig[64],
            };
            return xlGetDriverConfig(ref driverConfig) == XLstatus.SUCCESS;
        }

        private static int GetChannelCount(XLdriverConfig driverConfig)
        {
            return (int)Math.Min(driverConfig.channelCount, driverConfig.channel.Length);
        }

        /// <summary>
        /// 按硬件类型和硬件通道号查找 CAN 通道配置。
        /// </summary>
        internal static bool TryGetChannel(VectorHwType hwType, uint hwChannel, out XLchannelConfig channelConfig)
        {
            channelConfig = default(XLchannelConfig);
            if (!TryGetDriverConfig(out XLdriverConfig driverConfig)) return false;

            int count = GetChannelCount(driverConfig);
            for (int i = 0; i < count; i++)
            {
                XLchannelConfig config = driverConfig.channel[i];
                if (config.busType != XLbusType.CAN) continue;
                if ((VectorHwType)config.hwType == hwType && config.hwChannel == hwChannel)
                {
                    channelConfig = config;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 枚举指定硬件型号的所有 CAN 通道(供界面通道下拉选择;内部自动打开/关闭驱动,引用计数安全)。
        /// 驱动未安装或未连接对应硬件时返回空列表。
        /// </summary>
        public static List<XLchannelInfo> GetCanChannels(VectorHwType hwType)
        {
            List<XLchannelInfo> list = new List<XLchannelInfo>();
            try
            {
                if (OpenDriver() != XLstatus.SUCCESS) return list;
                try
                {
                    foreach (XLchannelInfo c in GetChannelInfos())
                        if (c.HwType == (byte)hwType && c.BusType == XLbusType.CAN) list.Add(c);
                }
                finally { CloseDriver(); }
            }
            catch { }
            return list;
        }

        /// <summary>枚举指定硬件型号的 CAN 通道并拼成诊断文本(用于错误提示)。</summary>
        internal static string GetCanChannelsText(VectorHwType hwType)
        {
            List<XLchannelInfo> available = GetCanChannels(hwType);
            return available.Count > 0
                ? string.Join(", ", available.Select(c => $"{c.Name}(hwChannel={c.HwChannel})"))
                : "(无,请检查驱动安装与硬件连接)";
        }

        private static string DecodeChannelName(byte[] raw)
        {
            if (raw == null) return string.Empty;
            int len = Array.IndexOf(raw, (byte)0);
            if (len < 0) len = raw.Length;
            return System.Text.Encoding.ASCII.GetString(raw, 0, len);
        }

        /// <summary>
        /// 枚举驱动中所有通道(诊断用,调用前须已 OpenDriver)。
        /// </summary>
        public static List<XLchannelInfo> GetChannelInfos()
        {
            List<XLchannelInfo> list = new List<XLchannelInfo>();
            if (!TryGetDriverConfig(out XLdriverConfig driverConfig)) return list;

            int count = GetChannelCount(driverConfig);
            for (int i = 0; i < count; i++)
            {
                XLchannelConfig c = driverConfig.channel[i];
                list.Add(new XLchannelInfo
                {
                    Name = DecodeChannelName(c.name),
                    HwType = c.hwType,
                    HwIndex = c.hwIndex,
                    HwChannel = c.hwChannel,
                    ChannelIndex = c.channelIndex,
                    ChannelMask = c.channelMask,
                    BusType = c.busType,
                    IsOnBus = c.isOnBus != 0,
                });
            }
            return list;
        }

        // busParams.data.can 布局: bitRate@0, sjw@4, tseg1@5, tseg2@6, sam@7, outputMode@8, canOpMode@16
        private static XLcanBusParamsInfo? ParseBusParams(XLchannelConfig c)
        {
            byte[] p = c.busParams;
            if (p == null || p.Length < 17) return null;
            return new XLcanBusParamsInfo
            {
                ChannelName = DecodeChannelName(c.name),
                BitRate = BitConverter.ToUInt32(p, 0),
                Sjw = p[4],
                Tseg1 = p[5],
                Tseg2 = p[6],
                Sam = p[7],
                OutputMode = p[8],
                CanOpMode = p[16],
            };
        }

        /// <summary>
        /// 按 channelMask 读回通道当前生效的经典 CAN 总线参数(诊断/校验用,调用前须已 OpenDriver)。
        /// </summary>
        public static bool TryReadBusParams(ulong channelMask, out XLcanBusParamsInfo? info)
        {
            info = null;
            if (!TryGetDriverConfig(out XLdriverConfig driverConfig)) return false;

            int count = GetChannelCount(driverConfig);
            for (int i = 0; i < count; i++)
            {
                XLchannelConfig c = driverConfig.channel[i];
                if (c.busType != XLbusType.CAN || c.channelMask != channelMask) continue;
                info = ParseBusParams(c);
                return info != null;
            }
            return false;
        }

        /// <summary>
        /// 按硬件类型和硬件通道号读回通道当前生效的经典 CAN 总线参数(诊断/校验用,调用前须已 OpenDriver)。
        /// </summary>
        public static bool TryReadBusParams(VectorHwType hwType, uint hwChannel, out XLcanBusParamsInfo? info)
        {
            info = null;
            if (!TryGetDriverConfig(out XLdriverConfig driverConfig)) return false;

            int count = GetChannelCount(driverConfig);
            for (int i = 0; i < count; i++)
            {
                XLchannelConfig c = driverConfig.channel[i];
                if (c.busType != XLbusType.CAN || (VectorHwType)c.hwType != hwType || c.hwChannel != hwChannel) continue;
                info = ParseBusParams(c);
                return info != null;
            }
            return false;
        }

        // CAN FD 仲裁段时序表:baudrate -> {tseg1, tseg2, sjw}(参考工程参数,80MHz时钟)
        private static readonly Dictionary<uint, uint[]> _fdNominalTiming = new Dictionary<uint, uint[]>
        {
            { 10000,   new uint[] { 16, 248, 1 } },
            { 20000,   new uint[] { 8, 248, 1 } },
            { 50000,   new uint[] { 4, 198, 1 } },
            { 100000,  new uint[] { 2, 198, 1 } },
            { 125000,  new uint[] { 2, 158, 1 } },
            { 250000,  new uint[] { 10, 11, 1 } },
            { 500000,  new uint[] { 119, 40, 40 } },
            { 800000,  new uint[] { 1, 48, 1 } },
            { 1000000, new uint[] { 59, 20, 20 } },
        };

        // CAN FD 数据段时序表:baudrate -> {tseg1, tseg2, sjw}(参考工程参数,80MHz时钟)
        private static readonly Dictionary<uint, uint[]> _fdDataTiming = new Dictionary<uint, uint[]>
        {
            { 500000,  new uint[] { 119, 40, 1 } },
            { 1000000, new uint[] { 59, 20, 20 } },
            { 2000000, new uint[] { 29, 10, 10 } },
            { 4000000, new uint[] { 14, 5, 5 } },
            { 5000000, new uint[] { 11, 4, 4 } },
            { 8000000, new uint[] { 5, 3, 3 } },
        };

        /// <summary>
        /// 按标准仲裁段/数据段波特率构建 CAN FD 配置。
        /// </summary>
        internal static XLcanFdConf BuildFdConf(uint arbitrationBitrate, uint dataBitrate, bool nonIso)
        {
            if (!_fdNominalTiming.TryGetValue(arbitrationBitrate, out uint[]? nom))
                throw new ArgumentException($"Unsupported arbitration bitrate: {arbitrationBitrate}");
            if (!_fdDataTiming.TryGetValue(dataBitrate, out uint[]? dat))
                throw new ArgumentException($"Unsupported data bitrate: {dataBitrate}");

            return new XLcanFdConf
            {
                arbitrationBitRate = arbitrationBitrate,
                tseg1Abr = nom[0],
                tseg2Abr = nom[1],
                sjwAbr = nom[2],
                dataBitRate = dataBitrate,
                tseg1Dbr = dat[0],
                tseg2Dbr = dat[1],
                sjwDbr = dat[2],
                options = nonIso ? (byte)0x08 : (byte)0,
            };
        }

        /// <summary>
        /// 按自定义寄存器参数构建 CAN FD 配置。
        /// </summary>
        internal static XLcanFdConf BuildFdConfCustom(uint arbitrationBitrate, int arbitrationTseg1, int arbitrationTseg2, int arbitrationSjw,
            uint dataBitrate, int dataTseg1, int dataTseg2, int dataSjw, bool nonIso)
        {
            return new XLcanFdConf
            {
                arbitrationBitRate = arbitrationBitrate,
                tseg1Abr = (uint)arbitrationTseg1,
                tseg2Abr = (uint)arbitrationTseg2,
                sjwAbr = (uint)arbitrationSjw,
                dataBitRate = dataBitrate,
                tseg1Dbr = (uint)dataTseg1,
                tseg2Dbr = (uint)dataTseg2,
                sjwDbr = (uint)dataSjw,
                options = nonIso ? (byte)0x08 : (byte)0,
            };
        }
    }
}
