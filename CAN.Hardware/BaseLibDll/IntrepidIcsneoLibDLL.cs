using System.Runtime.InteropServices;

namespace CAN.Hardware.BaseLibDll
{
    /// <summary>
    /// Intrepid neoVI API(icsneo40.dll) 互操作层。
    /// icsneo40.dll 随 Intrepid 驱动(Vehicle Spy 3 / Intrepid Product Drivers / neoAPI 安装包)
    /// 安装到系统目录(并注册 DLL 搜索路径),32/64 位进程均可使用,调用约定为 StdCall
    /// (.NET DllImport 默认 Winapi 即 StdCall,工程不随带 DLL)。
    /// 收发消息结构 icsSpyMessage(x64 布局 72 字节): 8 字节内联数据场 + ExtraDataPtr
    /// 外挂指针场,CAN FD 超过 8 字节时走 icsneoTxMessagesEx + ExtraDataPtr 通路。
    /// </summary>
    internal static class IntrepidIcsneoLibDLL
    {
        // icsneo40.dll 由 Intrepid 驱动安装,使用裸 DLL 名由系统搜索路径解析
        private const string DLL_NAME = "icsneo40.dll";

        /// <summary>
        /// 网络 ID(icsSpyMessage.NetworkID 的取值),高速 CAN 通道为 HSCAN1~HSCAN7。
        /// 枚举值与 icsnVC40.h 的 NETID_* 定义一致,仅保留 CAN 封装用到的部分。
        /// </summary>
        internal enum NetworkIDs
        {
            DEVICE = 0,
            HSCAN1 = 1,
            MSCAN = 2,
            SWCAN = 3,
            LSFTCAN = 4,
            HSCAN2 = 42,
            HSCAN3 = 44,
            HSCAN4 = 61,
            HSCAN5 = 62,
            HSCAN6 = 96,
            HSCAN7 = 97,
        }

        /// <summary>
        /// 设备类型(FindDevices 的过滤器),本封装传 0 表示全部类型。
        /// </summary>
        internal enum DeviceTypes : uint
        {
            UNKNOWN = 0x00000000,
            BLUE = 0x00000001,
            DW_VCAN = 0x00000004,
            RADMOON2 = 0x00000005,
            RADGIGALOG = 0x00000006,
            VCAN41 = 0x00000007,
            FIRE = 0x00000008,
            RADPLUTO = 0x00000009,
            VCAN42_EL = 0x0000000a,
            RADIO_CANHUB = 0x0000000b,
            NEOECU12 = 0x0000000c,
            VCAN3 = 0x00000010,
            RADJUPITER = 0x00000011,
            VCAN4_IND = 0x00000012,
            GIGASTAR = 0x00000013,
            RED = 0x00000040,
            OBD2_PRO = 0x00000400,
            PLASMA = 0x00001000,
            ION = 0x00040000,
            RADSTAR = 0x00080000,
            VCAN44 = 0x00200000,
            VCAN42 = 0x00400000,
            VCANRF = 0x02000000,
            FIRE2 = 0x04000000,
            FLEX = 0x08000000,
            RADGALAXY = 0x10000000,
            RADSTAR2 = 0x20000000,
            VIVIDCAN = 0x40000000,
            OBD2_SIM = 0x80000000,
            ALL = 0xFFFFBFFF,
        }

        /// <summary>
        /// 消息状态位(icsSpyMessage.StatusBitField)。
        /// </summary>
        [Flags]
        internal enum StatusBits : uint
        {
            NONE = 0,
            GLOBAL_ERR = 0x01,
            TX_MSG = 0x02,              // 发送(回显)消息
            XTD_FRAME = 0x04,           // 扩展帧(29-bit)
            REMOTE_FRAME = 0x08,        // 远程帧
            CRC_ERROR = 0x10,
            CAN_ERROR_PASSIVE = 0x20,
            INCOMPLETE_FRAME = 0x40,
            LOST_ARBITRATION = 0x80,
            UNDEFINED_ERROR = 0x100,
            CAN_BUS_OFF = 0x200,
            CAN_ERROR_WARNING = 0x400,
            BUS_SHORTED_PLUS = 0x800,
            BUS_SHORTED_GND = 0x1000,
            CHECKSUM_ERROR = 0x2000,
            BAD_MESSAGE_BIT_TIME_ERROR = 0x4000,
            IFR_DATA = 0x8000,
            COMM_IN_OVERFLOW = 0x10000, // 接收溢出(驱动/硬件缓冲)
            COMM_OUT_OVERFLOW = 0x20000,
            COMM_MISC_ERROR = 0x40000,
            BREAK = 0x80000,
            INIT_MESSAGE = 0x20000000,
            HIGH_SPEED = 0x40000000,
            EXTENDED = 0x80000000,      // 置位时 StatusBitField3 有效
        }

        /// <summary>
        /// 消息状态位 2(icsSpyMessage.StatusBitField2),仅保留 CAN 相关位。
        /// </summary>
        [Flags]
        internal enum StatusBits2 : uint
        {
            NONE = 0,
            HAS_VALUE = 1,
            VALUE_IS_BOOLEAN = 2,
            HIGH_VOLTAGE = 4,
            LONG_MESSAGE = 8,
            GLOBAL_CHANGE = 0x10000,
            ERROR_FRAME = 0x20000,      // CAN 错误帧
        }

        /// <summary>
        /// 消息状态位 3(icsSpyMessage.StatusBitField3),CAN FD 属性位。
        /// </summary>
        [Flags]
        internal enum StatusBits3 : uint
        {
            NONE = 0,
            CANFD_ESI = 0x01,           // 发送节点错误被动
            CANFD_IDE = 0x02,
            CANFD_RTR = 0x04,
            CANFD_FDF = 0x08,           // CAN FD 帧
            CANFD_BRS = 0x10,           // 数据段速率切换
        }

        /// <summary>
        /// 消息协议(icsSpyMessage.Protocol),收发过滤用。
        /// </summary>
        internal enum Protocol : byte
        {
            CUSTOM = 0,
            CAN = 1,
            GMLAN = 2,
            J1850VPW = 3,
            J1850PWM = 4,
            ISO9141 = 5,
            Keyword2000 = 6,
            GM_ALDL_UART = 7,
            CHRYSLER_CCD = 8,
            CHRYSLER_SCI = 9,
            FORD_UBP = 10,
            BEAN = 11,
            LIN = 12,
            J1708 = 13,
            CHRYSLER_JVPW = 14,
            J1939 = 15,
            FLEXRAY = 16,
            MOST = 17,
            CGI = 18,
            GME_CIM_SCL_KLINE = 19,
            SPI = 20,
            I2C = 21,
            GENERIC_UART = 22,
            JTAG = 23,
            UNIO = 24,
            DALLAS_1WIRE = 25,
            GENERIC_MANCHSESTER = 26,
            SENT_PROTOCOL = 27,
            UART = 28,
            ETHERNET = 29,
            CANFD = 30,
            GMFSA = 31,
            TCP = 32,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NeoDevice
        {
            public DeviceTypes DeviceType;
            public int Handle;
            public int NumberOfClients;
            public int SerialNumber;
            public int MaxAllowedClients;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NeoDeviceEx
        {
            public NeoDevice neoDevice;
            public uint FirmwareMajor;
            public uint FirmwareMinor;
            public uint Status;             // bit1 = CoreMini Running, bit2 = In BootLoader
            public uint Options;

            public IntPtr pAvailWIFINetwork;
            public IntPtr pWIFIInterfaceInfo;
            public int isEthernetDevice;

            public byte MACAddress0;
            public byte MACAddress1;
            public byte MACAddress2;
            public byte MACAddress3;
            public byte MACAddress4;
            public byte MACAddress5;

            public ushort hardwareRev;
            public ushort revReserved;

            public uint Reserved0;
            public uint Reserved1;
            public uint Reserved2;
            public uint Reserved3;
            public uint Reserved4;
            public uint Reserved5;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct OptionsNeoEx
        {
            public int CANOptions;
            public int Reserved00;
            public int Reserved01;
            public int Reserved02;
            public int Reserved03;
            public int Reserved04;
            public int Reserved05;
            public int Reserved06;
            public int Reserved07;
            public int Reserved08;
            public int Reserved09;
            public int Reserved10;
            public int Reserved11;
            public int Reserved12;
            public int Reserved13;
            public int Reserved14;
            public int Reserved15;
        }

        /// <summary>
        /// icsSpyMessage(x64 布局 72 字节): 与 icsnVC40.h 的 _icsSpyMessage 字段一一对应。
        /// NetworkID2 置于官方结构原位置(参考实现此处为对齐填充,布局完全一致);
        /// 8 字节内联数据场按 ulong 编组以保持结构体 blittable(GetMessages 批量取出零拷贝)。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct SpyMessage
        {
            public StatusBits StatusBitField;
            public StatusBits2 StatusBitField2;
            public int TimeHardware;            // 硬件时间戳低位(×1.6us)
            public int TimeHardware2;           // 硬件时间戳高位(×0.1048576s)
            public int TimeSystem;
            public int TimeSystem2;
            public byte TimeStampHardwareID;
            public byte TimeStampSystemID;
            public byte NetworkID;
            public byte NodeID;
            public Protocol Protocol;
            public byte MessagePieceID;
            public byte ExtraDataPtrEnabled;    // 置 1 时 iExtraDataPtr 指向外挂数据场
            public byte NumberBytesHeader;
            public byte NumberBytesData;        // 数据字节数(直接是长度,非 DLC 编码)
            public byte NetworkID2;             // 扩展网络 ID(NetworkID + NetworkID2*100)
            public short DescriptionID;
            public uint ArbIDOrHeader;          // 29-bit CAN 标识符
            public ulong Data;                  // 内联 8 字节数据场(小端: byte[0] 为 LSB)
            public StatusBits3 StatusBitField3;
            public int StatusBitField4;
            public IntPtr iExtraDataPtr;        // CAN FD >8 字节时的外挂数据指针
            public byte MiscData;
            public byte Reserved0;
            public byte Reserved1;
            public byte Reserved2;
        }

        // neoVI 硬件时间戳换算系数: 秒 = TimeHardware2*0.1048576 + TimeHardware*0.0000016
        internal const double NEOVI_TIMESTAMP_2 = 0.1048576;
        internal const double NEOVI_TIMESTAMP_1 = 0.0000016;

        /// <summary>
        /// 硬件时间戳(双 32 位)换算为工程统一的微秒。
        /// </summary>
        internal static ulong GetTimestampUs(int timeHardware, int timeHardware2)
        {
            return (ulong)((uint)timeHardware2 * NEOVI_TIMESTAMP_2 * 1e6 + (uint)timeHardware * NEOVI_TIMESTAMP_1 * 1e6);
        }

        [DllImport(DLL_NAME)]
        internal static extern int icsneoFindDevices(ref NeoDeviceEx pNeoDevice, ref int pNumDevices, uint DeviceTypes, uint numDeviceTypes, ref OptionsNeoEx pOptionsFindNeoEx, uint reserved);

        [DllImport(DLL_NAME)]
        internal static extern int icsneoOpenNeoDevice(ref NeoDevice pNeoDevice, ref IntPtr hObject, byte[] bNetworkIDs, int bConfigRead, int bSyncToPC);

        [DllImport(DLL_NAME)]
        internal static extern int icsneoClosePort(IntPtr hObject, ref int pNumberOfErrors);

        [DllImport(DLL_NAME)]
        internal static extern int icsneoSetBitRate(IntPtr hObject, int BitRate, NetworkIDs NetworkID);

        [DllImport(DLL_NAME)]
        internal static extern int icsneoSetFDBitRate(IntPtr hObject, int BitRate, NetworkIDs NetworkID);

        [DllImport(DLL_NAME)]
        internal static extern int icsneoGetMessages(IntPtr hObject, ref SpyMessage pMsg, ref int pNumberOfMessages, ref int pNumberOfErrors);

        [DllImport(DLL_NAME)]
        internal static extern int icsneoTxMessages(IntPtr hObject, ref SpyMessage pMsg, NetworkIDs lNetworkID, int lNumMessages);

        [DllImport(DLL_NAME)]
        internal static extern int icsneoTxMessagesEx(IntPtr hObject, ref SpyMessage pMsg, NetworkIDs lNetworkID, int lNumMessages, ref uint NumTxed, uint reserved);

        [DllImport(DLL_NAME)]
        internal static extern int icsneoWaitForRxMessagesWithTimeOut(IntPtr hObject, uint iTimeOut);

        /// <summary>
        /// 查找第一台已连接的 Intrepid 设备并打开,返回设备句柄。
        /// DeviceTypes/numDeviceTypes 传 0 表示不过滤设备类型;bConfigRead=1 由 DLL 先读出设备配置。
        /// </summary>
        internal static IntPtr OpenFirstDevice()
        {
            var device = new NeoDeviceEx();
            var options = new OptionsNeoEx();
            int numDevices = 1;
            int result = icsneoFindDevices(ref device, ref numDevices, 0, 0, ref options, 0);
            if (result != 1)
                throw new Exception($"Failed({result}) call to icsneoFindDevices,请确认已安装 Intrepid 驱动并连接设备");
            if (numDevices < 1)
                throw new Exception("No connected Intrepid devices found");

            IntPtr handle = IntPtr.Zero;
            if ((result = icsneoOpenNeoDevice(ref device.neoDevice, ref handle, null, 1, 0)) != 1)
                throw new Exception($"Failed({result}) call to icsneoOpenNeoDevice");
            return handle;
        }
    }
}
