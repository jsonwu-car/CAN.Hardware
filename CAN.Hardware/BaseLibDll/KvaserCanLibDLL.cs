using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using static CAN.Hardware.BaseLibDll.Canlib;
namespace CAN.Hardware.BaseLibDll
{
    //
    // 摘要:
    //     Returned when using Kvaser.CanLib.Canlib.canCHANNELDATA_CLOCK_INFO Returns clock
    //     characteristics for device The device clock frequency can then be calculated
    //     as: double frequency = numerator/denominator * 10 ** power_of_ten;
    public struct kvClockInfo
    {
        //
        // 摘要:
        //     The version of this struct, currently 1
        public int version { get; set; }

        //
        // 摘要:
        //     The numerator part of the device clock frequency.
        public int numerator { get; set; }

        //
        // 摘要:
        //     The denominator part of the device clock frequency.
        public int denominator { get; set; }

        //
        // 摘要:
        //     The power_of_ten part of the device clock frequency.
        public int power_of_ten { get; set; }

        //
        // 摘要:
        //     The accuracy (in ppm) of the device clock.
        public int accuracy_ppm { get; set; }
    }
    public struct kvBusParamLimits
    {
        //
        // 摘要:
        //     The version of this struct, currently 2
        public int version { get; set; }

        //
        // 摘要:
        //     Arbitration phase minimum parameters.
        public kvBusParamsTq arbitration_min { get; set; }

        //
        // 摘要:
        //     Arbitration phase maximum parameters.
        public kvBusParamsTq arbitration_max { get; set; }

        //
        // 摘要:
        //     Data phase minimum parameters.
        public kvBusParamsTq data_min { get; set; }

        //
        // 摘要:
        //     Data phase maximum parameters.
        public kvBusParamsTq data_max { get; set; }
    }
    public struct canChannelDataHwStatus
    {
        //
        // 摘要:
        //     HW Status1
        public int HwStatus1 { get; set; }

        //
        // 摘要:
        //     HW Status2
        public int HwStatus2 { get; set; }

        //
        // 摘要:
        //     HW Status3
        public int HwStatus3 { get; set; }

        //
        // 摘要:
        //     HW Status4
        public int HwStatus4 { get; set; }

        //
        // 摘要:
        //     HW Status5
        public int HwStatus5 { get; set; }

        //
        // 摘要:
        //     HW Status6
        public int HwStatus6 { get; set; }

        //
        // 摘要:
        //     Conversion operator returning Int32[] buffer
        public static implicit operator int[](canChannelDataHwStatus status)
        {
            return new int[6] { status.HwStatus1, status.HwStatus2, status.HwStatus3, status.HwStatus4, status.HwStatus5, status.HwStatus6 };
        }
    }
    public struct kvBusParamsTq
    {
        public int tq { get; set; }

        public int phase1 { get; set; }

        public int phase2 { get; set; }

        public int sjw { get; set; }

        public int prop { get; set; }

        public int prescaler { get; set; }

        public kvBusParamsTq(int tq, int phase1, int phase2, int sjw, int prop, int prescaler)
        {
            this.tq = tq;
            this.phase1 = phase1;
            this.phase2 = phase2;
            this.sjw = sjw;
            this.prop = prop;
            this.prescaler = prescaler;
        }

        public void SetValues(int tq, int phase1, int phase2, int sjw, int prop, int prescaler)
        {
            this.tq = tq;
            this.phase1 = phase1;
            this.phase2 = phase2;
            this.sjw = sjw;
            this.prop = prop;
            this.prescaler = prescaler;
        }
    }
    public struct canChannelDataVersion
    {
        //
        // 摘要:
        //     Version1
        public int Version1 { get; set; }

        //
        // 摘要:
        //     Version2
        public int Version2 { get; set; }

        //
        // 摘要:
        //     Version3
        public int Version3 { get; set; }

        //
        // 摘要:
        //     Version4
        public int Version4 { get; set; }

        //
        // 摘要:
        //     Conversion operator returning Int32[] buffer
        public static implicit operator int[](canChannelDataVersion dataVersion)
        {
            return new int[4] { dataVersion.Version1, dataVersion.Version2, dataVersion.Version3, dataVersion.Version4 };
        }
    }
    public static class Canlib
    {
        public enum canStatus
        {
            [Description("Succed")]
            canOK = 0,
            [Description("Error in one or more parameters")]
            canERR_PARAM = -1,
            [Description("There no messages to read")]
            canERR_NOMSG = -2,
            [Description("Specifiled device or channel not found")]
            canERR_NOTFOUND = -3,
            [Description("Out of Memory, A memory allocation failed")]
            canERR_NOMEM = -4,
            [Description("No channels available")]
            canERR_NOCHANNELS = -5,
            [Description("Interrupted by signals")]
            canERR_INTERRUPTED = -6,
            [Description("Timeout occurred")]
            canERR_TIMEOUT = -7,
            [Description("The libraty is not initialized")]
            canERR_NOTINITIALIZED = -8,
            [Description("Out of Handels")]
            canERR_NOHANDLES = -9,
            [Description("Handle is ivalid")]
            canERR_INVHANDLE = -10,
            [Description("Error in the ini-file")]
            canERR_INIFILE = -11,
            [Description("Driver type not supported")]
            canERR_DRIVER = -12,
            [Description("Transimit buffer overflow")]
            canERR_TXBUFOFL = -13,
            [Description("Reserved")]
            canERR_RESERVED_1 = -14,
            [Description("A hardware error has occurred")]
            canERR_HARDWARE = -15,
            [Description("A driver dll can't be found or loaded")]
            canERR_DYNALOAD = -16,
            [Description("A dll seems to have wrong version")]
            canERR_DYNALIB = -17,
            [Description("Error when initializing a dll")]
            canERR_DYNAINIT = -18,
            [Description("Operation not supported by hardware or firmware")]
            canERR_NOT_SUPPORTED = -19,
            [Description("Reserved")]
            canERR_RESERVED_5 = -20,
            [Description("Reserved")]
            canERR_RESERVED_6 = -21,
            [Description("Reserved")]
            canERR_RESERVED_2 = -22,
            [Description("Can't find or load kernel driver")]
            canERR_DRIVERLOAD = -23,
            [Description("Device IO Control failed")]
            canERR_DRIVERFAILED = -24,
            [Description("Can't find req'd config s/w")]
            canERR_NOCONFIGMGR = -25,
            [Description("The card was removed or not inserted")]
            canERR_NOCARD = -26,
            [Description("Reserved")]
            canERR_RESERVED_7 = -27,
            [Description("Error in the registry")]
            canERR_REGISTRY = -28,
            [Description("The license is not valid")]
            canERR_LICENSE = -29,
            [Description("Internal error in the driver")]
            canERR_INTERNAL = -30,
            [Description("Access denied")]
            canERR_NO_ACCESS = -31,
            [Description("Not implemented")]
            canERR_NOT_IMPLEMENTED = -32,
            [Description("Device file error")]
            canERR_DEVICE_FILE = -33,
            [Description("Host file error")]
            canERR_HOST_FILE = -34,
            [Description("Disk error")]
            canERR_DISK = -35,
            [Description("CRC error")]
            canERR_CRC = -36,
            [Description("Configuration error")]
            canERR_CONFIG = -37,
            [Description("Other configuration error")]
            canERR_MEMO_FAIL = -38,
            [Description("A script ha failed")]
            canERR_SCRIPT_FAIL = -39,
            [Description("The t script version dosen't match the version that the devices firmware supports")]
            canERR_SCRIPT_WRONG_VERSION = -40,
            [Description("The compiled t script container file format is of a version which is not supported by this version of canlib")]
            canERR_SCRIPT_TXE_CONTAINER_VERSION = -41,
            [Description("An error occured while trying to parse the compiled t script file")]
            canERR_SCRIPT_TXE_CONTAINER_FORMAT = -42,
            [Description("The buffer provided was not large enough to contain the requested data")]
            canERR_BUFFER_TOO_SMALL = -43,
            [Description("The I/O pin doesn't exist or the I/O pin type doesn't match the called function")]
            canERR_IO_WRONG_PIN_TYPE = -44,
            [Description("The I/O pin configuration is not confirmed")]
            canERR_IO_NOT_CONFIRMED = -45,
            [Description("The I/O pin configuration has changed after last cal to kvIoConfirmConfig")]
            canERR_IO_CONFIG_CHANGED = -46,
            [Description("The previous I/O pin value has not yet changed the output and is still pending")]
            canERR_IO_PENDING = -47,
            [Description("The is no valid I/O pin configuration")]
            canERR_IO_NO_VALID_CONFIG = -48,
            [Description("Reserved")]
            canERR__RESERVED = -49
        }
        public delegate canStatus CanGetChannelOrHandleDataDelegate(int channelOrHandle, int item, IntPtr buffer, UIntPtr bufsize);
        public enum canIoCtl { 
            canIOCTL_PREFER_EXT =  1, 
             canIOCTL_PREFER_STD =  2, 
             canIOCTL_CLEAR_ERROR_COUNTERS=   5, 
             canIOCTL_SET_TIMER_SCALE =  6, 
             canIOCTL_SET_TXACK  = 7, 
             canIOCTL_GET_RX_BUFFER_LEVEL  = 8, 
             canIOCTL_GET_TX_BUFFER_LEVEL  = 9, 
             canIOCTL_FLUSH_RX_BUFFER  = 10, 
             canIOCTL_FLUSH_TX_BUFFER  = 11, 
             canIOCTL_GET_TIMER_SCALE  = 12, 
             canIOCTL_SET_TXRQ  = 13, 
             canIOCTL_GET_EVENTHANDLE  = 14, 
             canIOCTL_SET_BYPASS_MODE  = 15, 
             canIOCTL_SET_WAKEUP  = 16, 
             canIOCTL_GET_DRIVERHANDLE  = 17, 
             canIOCTL_MAP_RXQUEUE  = 18, 
             canIOCTL_GET_WAKEUP  = 19, 
             canIOCTL_SET_REPORT_ACCESS_ERRORS  = 20, 
             canIOCTL_GET_REPORT_ACCESS_ERRORS  = 21, 
             canIOCTL_CONNECT_TO_VIRTUAL_BUS  = 22, 
             canIOCTL_DISCONNECT_FROM_VIRTUAL_BUS  = 23, 
             canIOCTL_SET_USER_IOPORT  = 24, 
             canIOCTL_GET_USER_IOPORT  = 25, 
             canIOCTL_SET_BUFFER_WRAPAROUND_MODE  = 26, 
             canIOCTL_SET_RX_QUEUE_SIZE  = 27, 
             canIOCTL_SET_USB_THROTTLE  = 28, 
             canIOCTL_GET_USB_THROTTLE  = 29, 
             canIOCTL_SET_BUSON_TIME_AUTO_RESET  = 30, 
             canIOCTL_GET_TXACK  = 31, 
             canIOCTL_SET_LOCAL_TXECHO  = 32, 
             canIOCTL_SET_ERROR_FRAMES_REPORTING  = 33, 
             canIOCTL_GET_CHANNEL_QUALITY  = 34, 
             canIOCTL_GET_ROUNDTRIP_TIME  = 35, 
             canIOCTL_GET_BUS_TYPE  = 36, 
             canIOCTL_GET_DEVNAME_ASCII  = 37, 
             canIOCTL_GET_TIME_SINCE_LAST_SEEN  = 38, 
             canIOCTL_GET_TREF_LIST  = 39, 
             canIOCTL_TX_INTERVAL  = 40, 
             canIOCTL_SET_BRLIMIT  = 43, 
             canIOCTL_SET_USB_THROTTLE_SCALED  = 41, 
             canIOCTL_SET_THROTTLE_SCALED  = 41, 
             canIOCTL_GET_USB_THROTTLE_SCALED  = 42, 
             canIOCTL_GET_THROTTLE_SCALED  = 42, 
             canIOCTL_RESET_OVERRUN_COUNT  = 44, 
             canIOCTL_LIN_MODE  = 45, 
             canIOCTL_SET_LOCAL_TXACK  = 46, 
        }
    }
    public class canUserIoPortData
    {
        public int portNo;

        public int portValue;
    }


    internal static class KvaserCanLibDLL
    {
        public struct canUserIoPortData
        {
            public uint portNo;

            public uint portValue;
        }
        public static readonly UIntPtr MAX_STRING_SIZE = (UIntPtr)1024uL;

        public const int canEVENT_RX = 32000;

        public const int canEVENT_TX = 32001;

        public const int canEVENT_ERROR = 32002;

        public const int canEVENT_STATUS = 32003;

        public const int canEVENT_ENVVAR = 32004;

        public const int canEVENT_BUSONOFF = 32005;

        public const int canEVENT_REMOVED = 32006;

        public const int canNOTIFY_NONE = 0;

        public const int canNOTIFY_RX = 1;

        public const int canNOTIFY_TX = 2;

        public const int canNOTIFY_ERROR = 4;

        public const int canNOTIFY_STATUS = 8;

        public const int canNOTIFY_ENVVAR = 16;

        public const int canNOTIFY_BUSONOFF = 32;

        public const int canNOTIFY_REMOVED = 64;

        public const int canSTAT_ERROR_PASSIVE = 1;

        public const int canSTAT_BUS_OFF = 2;

        public const int canSTAT_ERROR_WARNING = 4;

        public const int canSTAT_ERROR_ACTIVE = 8;

        public const int canSTAT_TX_PENDING = 16;

        public const int canSTAT_RX_PENDING = 32;

        public const int canSTAT_RESERVED_1 = 64;

        public const int canSTAT_TXERR = 128;

        public const int canSTAT_RXERR = 256;

        public const int canSTAT_HW_OVERRUN = 512;

        public const int canSTAT_SW_OVERRUN = 1024;

        public const int canSTAT_OVERRUN = 1536;

        public const int canMSG_MASK = 255;

        public const int canMSG_RTR = 1;

        //public const int canMSG_STD = 2;

        //public const int canMSG_EXT = 4;

        public const int canMSG_WAKEUP = 8;

        public const int canMSG_NERR = 16;

        public const int canMSG_ERROR_FRAME = 32;

        public const int canMSG_TXACK = 64;

        public const int canMSG_TXRQ = 128;

        public const int canMSGERR_MASK = 65280;

        public const int canMSGERR_HW_OVERRUN = 512;

        public const int canMSGERR_SW_OVERRUN = 1024;

        public const int canMSGERR_STUFF = 2048;

        public const int canMSGERR_FORM = 4096;

        public const int canMSGERR_CRC = 8192;

        public const int canMSGERR_BIT0 = 16384;

        public const int canMSGERR_BIT1 = 32768;

        public const int canMSGERR_OVERRUN = 1536;

        public const int canMSGERR_BIT = 49152;

        public const int canMSGERR_BUSERR = 63488;

        public const int canFDMSG_MASK = 16711680;

        public const int canFDMSG_EDL = 65536;

        //public const int canFDMSG_FDF = 65536;

        //public const int canFDMSG_BRS = 131072;

        public const int canFDMSG_ESI = 262144;

        public const int canTRANSCEIVER_LINEMODE_NA = 0;

        public const int canTRANSCEIVER_LINEMODE_SWC_SLEEP = 4;

        public const int canTRANSCEIVER_LINEMODE_SWC_NORMAL = 5;

        public const int canTRANSCEIVER_LINEMODE_SWC_FAST = 6;

        public const int canTRANSCEIVER_LINEMODE_SWC_WAKEUP = 7;

        public const int canTRANSCEIVER_LINEMODE_SLEEP = 8;

        public const int canTRANSCEIVER_LINEMODE_NORMAL = 9;

        public const int canTRANSCEIVER_LINEMODE_STDBY = 10;

        public const int canTRANSCEIVER_LINEMODE_TT_CAN_H = 11;

        public const int canTRANSCEIVER_LINEMODE_TT_CAN_L = 12;

        public const int canTRANSCEIVER_LINEMODE_OEM1 = 13;

        public const int canTRANSCEIVER_LINEMODE_OEM2 = 14;

        public const int canTRANSCEIVER_LINEMODE_OEM3 = 15;

        public const int canTRANSCEIVER_LINEMODE_OEM4 = 16;

        public const int canTRANSCEIVER_RESNET_NA = 0;

        public const int canTRANSCEIVER_RESNET_MASTER = 1;

        public const int canTRANSCEIVER_RESNET_MASTER_STBY = 2;

        public const int canTRANSCEIVER_RESNET_SLAVE = 3;

        public const int canTRANSCEIVER_TYPE_UNKNOWN = 0;

        public const int canTRANSCEIVER_TYPE_251 = 1;

        public const int canTRANSCEIVER_TYPE_252 = 2;

        public const int canTRANSCEIVER_TYPE_DNOPTO = 3;

        public const int canTRANSCEIVER_TYPE_W210 = 4;

        public const int canTRANSCEIVER_TYPE_SWC_PROTO = 5;

        public const int canTRANSCEIVER_TYPE_SWC = 6;

        public const int canTRANSCEIVER_TYPE_EVA = 7;

        public const int canTRANSCEIVER_TYPE_FIBER = 8;

        public const int canTRANSCEIVER_TYPE_K251 = 9;

        public const int canTRANSCEIVER_TYPE_K = 10;

        public const int canTRANSCEIVER_TYPE_1054_OPTO = 11;

        public const int canTRANSCEIVER_TYPE_SWC_OPTO = 12;

        public const int canTRANSCEIVER_TYPE_TT = 13;

        public const int canTRANSCEIVER_TYPE_1050 = 14;

        public const int canTRANSCEIVER_TYPE_1050_OPTO = 15;

        public const int canTRANSCEIVER_TYPE_1041 = 16;

        public const int canTRANSCEIVER_TYPE_1041_OPTO = 17;

        public const int canTRANSCEIVER_TYPE_RS485 = 18;

        public const int canTRANSCEIVER_TYPE_LIN = 19;

        public const int canTRANSCEIVER_TYPE_KONE = 20;

        public const int canTRANSCEIVER_TYPE_CANFD = 22;

        public const int canTRANSCEIVER_TYPE_CANFD_LIN = 24;

        public const int canTRANSCEIVER_TYPE_LINX_LIN = 64;

        public const int canTRANSCEIVER_TYPE_LINX_J1708 = 66;

        public const int canTRANSCEIVER_TYPE_LINX_K = 68;

        public const int canTRANSCEIVER_TYPE_LINX_SWC = 70;

        public const int canTRANSCEIVER_TYPE_LINX_LS = 72;

        public const int canINVALID_HANDLE = -1;

        public const int WM__CANLIB = 17378;

        public const int canWANT_EXCLUSIVE = 8;

        public const int canWANT_EXTENDED = 16;

        public const int canWANT_VIRTUAL = 32;

        //public const int canOPEN_EXCLUSIVE = 8;

        public const int canOPEN_REQUIRE_EXTENDED = 16;

        public const int canOPEN_ACCEPT_VIRTUAL = 32;

        public const int canOPEN_OVERRIDE_EXCLUSIVE = 64;

        public const int canOPEN_REQUIRE_INIT_ACCESS = 128;

        public const int canOPEN_NO_INIT_ACCESS = 256;

        public const int canOPEN_ACCEPT_LARGE_DLC = 512;

        public const int canOPEN_CAN_FD = 1024;

        public const int canOPEN_CAN_FD_NONISO = 2048;

        public const int canFILTER_ACCEPT = 1;

        public const int canFILTER_REJECT = 2;

        public const int canFILTER_SET_CODE_STD = 3;

        public const int canFILTER_SET_MASK_STD = 4;

        public const int canFILTER_SET_CODE_EXT = 5;

        public const int canFILTER_SET_MASK_EXT = 6;

        public const int canFILTER_NULL_MASK = 0;

        public const int canDRIVER_NORMAL = 4;

        public const int canDRIVER_SILENT = 1;

        public const int canDRIVER_SELFRECEPTION = 8;

        public const int canDRIVER_OFF = 0;

        public const int canBITRATE_1M = -1;

        public const int canBITRATE_500K = -2;

        public const int canBITRATE_250K = -3;

        public const int canBITRATE_125K = -4;

        public const int canBITRATE_100K = -5;

        public const int canBITRATE_62K = -6;

        public const int canBITRATE_50K = -7;

        public const int canBITRATE_83K = -8;

        public const int canBITRATE_10K = -9;

        public const int canFD_BITRATE_500K_80P = -1000;

        public const int canFD_BITRATE_1M_80P = -1001;

        public const int canFD_BITRATE_2M_80P = -1002;

        public const int canFD_BITRATE_4M_80P = -1003;

        public const int canFD_BITRATE_8M_60P = -1004;

        public const int BAUD_1M = -1;

        public const int BAUD_500K = -2;

        public const int BAUD_250K = -3;

        public const int BAUD_125K = -4;

        public const int BAUD_100K = -5;

        public const int BAUD_62K = -6;

        public const int BAUD_50K = -7;

        public const int BAUD_83K = -8;

        public const int CANID_METAMSG = -1;

        public const int CANID_WILDCARD = -2;
        public const int canCHANNELDATA_CHANNEL_CAP = 1;

        public const int canCHANNELDATA_TRANS_CAP = 2;

        public const int canCHANNELDATA_CHANNEL_FLAGS = 3;

        public const int canCHANNELDATA_CARD_TYPE = 4;

        public const int canCHANNELDATA_CARD_NUMBER = 5;

        public const int canCHANNELDATA_CHAN_NO_ON_CARD = 6;

        public const int canCHANNELDATA_CARD_SERIAL_NO = 7;

        public const int canCHANNELDATA_TRANS_SERIAL_NO = 8;

        public const int canCHANNELDATA_CARD_FIRMWARE_REV = 9;

        public const int canCHANNELDATA_CARD_HARDWARE_REV = 10;

        public const int canCHANNELDATA_CARD_UPC_NO = 11;

        public const int canCHANNELDATA_TRANS_UPC_NO = 12;

        public const int canCHANNELDATA_CHANNEL_NAME = 13;

        public const int canCHANNELDATA_DLL_FILE_VERSION = 14;

        public const int canCHANNELDATA_DLL_PRODUCT_VERSION = 15;

        public const int canCHANNELDATA_DLL_FILETYPE = 16;

        public const int canCHANNELDATA_TRANS_TYPE = 17;

        public const int canCHANNELDATA_DEVICE_PHYSICAL_POSITION = 18;

        public const int canCHANNELDATA_UI_NUMBER = 19;

        public const int canCHANNELDATA_TIMESYNC_ENABLED = 20;

        public const int canCHANNELDATA_DRIVER_FILE_VERSION = 21;

        public const int canCHANNELDATA_DRIVER_PRODUCT_VERSION = 22;

        public const int canCHANNELDATA_MFGNAME_UNICODE = 23;

        public const int canCHANNELDATA_MFGNAME_ASCII = 24;

        public const int canCHANNELDATA_DEVDESCR_UNICODE = 25;

        public const int canCHANNELDATA_DEVDESCR_ASCII = 26;

        public const int canCHANNELDATA_DRIVER_NAME = 27;

        public const int canCHANNELDATA_CHANNEL_QUALITY = 28;

        public const int canCHANNELDATA_ROUNDTRIP_TIME = 29;

        public const int canCHANNELDATA_BUS_TYPE = 30;

        public const int canCHANNELDATA_DEVNAME_ASCII = 31;

        public const int canCHANNELDATA_TIME_SINCE_LAST_SEEN = 32;

        public const int canCHANNELDATA_REMOTE_OPERATIONAL_MODE = 33;

        public const int canCHANNELDATA_REMOTE_PROFILE_NAME = 34;

        public const int canCHANNELDATA_REMOTE_HOST_NAME = 35;

        public const int canCHANNELDATA_REMOTE_MAC = 36;

        public const int canCHANNELDATA_MAX_BITRATE = 37;

        public const int canCHANNELDATA_CHANNEL_CAP_MASK = 38;

        public const int canCHANNELDATA_IS_REMOTE = 40;

        public const int canCHANNELDATA_REMOTE_TYPE = 41;

        public const int canCHANNELDATA_LOGGER_TYPE = 42;

        public const int canCHANNELDATA_HW_STATUS = 43;

        public const int canCHANNELDATA_FEATURE_EAN = 44;

        public const int canCHANNELDATA_BUS_PARAM_LIMITS = 45;

        public const int canCHANNELDATA_CLOCK_INFO = 46;

        public const int canCHANNELDATA_CHANNEL_CAP_EX = 47;

        public const int canCHANNELDATA_CUST_CHANNEL_NAME = 39;

        public const int canCHANNEL_IS_EXCLUSIVE = 1;

        public const int canCHANNEL_IS_OPEN = 2;

        public const int canCHANNEL_IS_CANFD = 4;

        public const int canCHANNEL_IS_LIN = 16;

        public const int canCHANNEL_IS_LIN_MASTER = 32;

        public const int canCHANNEL_IS_LIN_SLAVE = 64;

        //public const int canDRIVER_OFF = 0;

        //public const int canDRIVER_SILENT = 1;

        //public const int canDRIVER_NORMAL = 4;

        //public const int canFILTER_NULL_MASK = 0;

        //public const int canFILTER_ACCEPT = 1;

        //public const int canFILTER_REJECT = 2;

        //public const int canFILTER_SET_CODE_STD = 3;

        //public const int canFILTER_SET_MASK_STD = 4;

        //public const int canFILTER_SET_CODE_EXT = 5;

        //public const int canFILTER_SET_MASK_EXT = 6;

        public const int canHWTYPE_NONE = 0;

        public const int canHWTYPE_VIRTUAL = 1;

        public const int canHWTYPE_LAPCAN = 2;

        public const int canHWTYPE_CANPARI = 3;

        public const int canHWTYPE_PCCAN = 8;

        public const int canHWTYPE_PCICAN = 9;

        public const int canHWTYPE_USBCAN = 11;

        public const int canHWTYPE_PCICAN_II = 40;

        public const int canHWTYPE_USBCAN_II = 42;

        public const int canHWTYPE_SIMULATED = 44;

        public const int canHWTYPE_ACQUISITOR = 46;

        public const int canHWTYPE_LEAF = 48;

        public const int canHWTYPE_PC104_PLUS = 50;

        public const int canHWTYPE_PCICANX_II = 52;

        public const int canHWTYPE_MEMORATOR_II = 54;

        public const int canHWTYPE_MEMORATOR_PRO = 54;

        public const int canHWTYPE_USBCAN_PRO = 56;

        public const int canHWTYPE_IRIS = 58;

        public const int canHWTYPE_BLACKBIRD = 58;

        public const int canHWTYPE_MEMORATOR_LIGHT = 60;

        public const int canHWTYPE_MINIHYDRA = 62;

        public const int canHWTYPE_EAGLE = 62;

        public const int canHWTYPE_BAGEL = 64;

        public const int canHWTYPE_BLACKBIRD_V2 = 64;

        public const int canHWTYPE_MINIPCIE = 66;

        public const int canHWTYPE_USBCAN_KLINE = 68;

        public const int canHWTYPE_ETHERCAN = 70;

        public const int canHWTYPE_USBCAN_LIGHT = 72;

        public const int canHWTYPE_USBCAN_PRO2 = 74;

        public const int canHWTYPE_PCIE_V2 = 76;

        public const int canHWTYPE_MEMORATOR_PRO2 = 78;

        public const int canHWTYPE_LEAF2 = 80;

        public const int canHWTYPE_MEMORATOR_V2 = 82;

        public const int canHWTYPE_CANLINHYBRID = 84;

        public const int canHWTYPE_DINRAIL = 86;

        public const int canHWTYPE_U100 = 88;

        public const int canHWTYPE_LEAF3 = 90;

        public const int canCHANNEL_CAP_EXTENDED_CAN = 1;

        public const int canCHANNEL_CAP_BUS_STATISTICS = 2;

        public const int canCHANNEL_CAP_ERROR_COUNTERS = 4;

        public const int canCHANNEL_CAP_RESERVED_2 = 8;

        public const int canCHANNEL_CAP_GENERATE_ERROR = 16;

        public const int canCHANNEL_CAP_GENERATE_OVERLOAD = 32;

        public const int canCHANNEL_CAP_TXREQUEST = 64;

        public const int canCHANNEL_CAP_TXACKNOWLEDGE = 128;

        public const int canCHANNEL_CAP_VIRTUAL = 65536;

        public const int canCHANNEL_CAP_SIMULATED = 131072;

        public const int canCHANNEL_CAP_RESERVED_1 = 262144;

        public const int canCHANNEL_CAP_CAN_FD = 524288;

        public const int canCHANNEL_CAP_CAN_FD_NONISO = 1048576;

        public const int canCHANNEL_CAP_SILENT_MODE = 2097152;

        public const int canCHANNEL_CAP_SINGLE_SHOT = 4194304;

        public const int canCHANNEL_CAP_LOGGER = 8388608;

        public const int canCHANNEL_CAP_REMOTE_ACCESS = 16777216;

        public const int canCHANNEL_CAP_SCRIPT = 33554432;

        public const int canCHANNEL_CAP_LIN_HYBRID = 67108864;

        public const int canCHANNEL_CAP_IO_API = 134217728;

        public const int canCHANNEL_CAP_DIAGNOSTICS = 268435456;

        public const long canCHANNEL_CAP_EX_BUSPARAMS_TQ = 1L;

        public const int canCHANNEL_OPMODE_NONE = 1;

        public const int canCHANNEL_OPMODE_INFRASTRUCTURE = 2;

        public const int canCHANNEL_OPMODE_RESERVED = 3;

        public const int canCHANNEL_OPMODE_ADHOC = 4;

        public const int canDRIVER_CAP_HIGHSPEED = 1;

        //public const int canEVENT_RX = 32000;

        //public const int canEVENT_TX = 32001;

        //public const int canEVENT_ERROR = 32002;

        //public const int canEVENT_STATUS = 32003;

        //public const int canEVENT_ENVVAR = 32004;

        //public const int canEVENT_BUSONOFF = 32005;

        //public const int canEVENT_REMOVED = 32006;

        public const int canOPEN_EXCLUSIVE = 8;

        //public const int canOPEN_REQUIRE_EXTENDED = 16;

        //public const int canOPEN_ACCEPT_VIRTUAL = 32;

        //public const int canOPEN_OVERRIDE_EXCLUSIVE = 64;

        //public const int canOPEN_REQUIRE_INIT_ACCESS = 128;

        //public const int canOPEN_NO_INIT_ACCESS = 256;

        //public const int canOPEN_ACCEPT_LARGE_DLC = 512;

        //public const int canOPEN_CAN_FD = 1024;

        //public const int canOPEN_CAN_FD_NONISO = 2048;

        //public const int canMSG_MASK = 255;

        //public const int canMSG_RTR = 1;

        public const int canMSG_STD = 2;

        public const int canMSG_EXT = 4;

        //public const int canMSG_WAKEUP = 8;

        //public const int canMSG_NERR = 16;

        //public const int canMSG_ERROR_FRAME = 32;

        //public const int canMSG_TXACK = 64;

        //public const int canMSG_TXRQ = 128;

        public const int canMSG_DELAY_MSG = 256;

        public const int canMSG_LOCAL_TXACK = 268435456;

        public const int canMSG_SINGLE_SHOT = 16777216;

        public const int canMSG_TXNACK = 33554432;

        public const int canMSG_ABL = 67108864;

        //public const int canMSGERR_MASK = 65280;

        //public const int canMSGERR_HW_OVERRUN = 512;

        //public const int canMSGERR_SW_OVERRUN = 1024;

        //public const int canMSGERR_STUFF = 2048;

        //public const int canMSGERR_FORM = 4096;

        //public const int canMSGERR_CRC = 8192;

        //public const int canMSGERR_BIT0 = 16384;

        //public const int canMSGERR_BIT1 = 32768;

        //public const int canMSGERR_OVERRUN = 1536;

        //public const int canMSGERR_BIT = 49152;

        //public const int canMSGERR_BUSERR = 63488;

        //public const int canFDMSG_MASK = 16711680;

        public const int canFDMSG_FDF = 65536;

        public const int canFDMSG_BRS = 131072;

        //public const int canFDMSG_ESI = 262144;

        //public const int canSTAT_ERROR_PASSIVE = 1;

        //public const int canSTAT_BUS_OFF = 2;

        //public const int canSTAT_ERROR_WARNING = 4;

        //public const int canSTAT_ERROR_ACTIVE = 8;

        //public const int canSTAT_TX_PENDING = 16;

        //public const int canSTAT_RX_PENDING = 32;

        //public const int canSTAT_RESERVED_1 = 64;

        //public const int canSTAT_TXERR = 128;

        //public const int canSTAT_RXERR = 256;

        //public const int canSTAT_HW_OVERRUN = 512;

        //public const int canSTAT_SW_OVERRUN = 1024;

        //public const int canSTAT_OVERRUN = 1536;

        public const int canIOCTL_PREFER_EXT = 1;

        public const int canIOCTL_PREFER_STD = 2;

        public const int canIOCTL_CLEAR_ERROR_COUNTERS = 5;

        public const int canIOCTL_SET_TIMER_SCALE = 6;

        public const int canIOCTL_SET_TXACK = 7;

        public const int canIOCTL_GET_RX_BUFFER_LEVEL = 8;

        public const int canIOCTL_GET_TX_BUFFER_LEVEL = 9;

        public const int canIOCTL_FLUSH_RX_BUFFER = 10;

        public const int canIOCTL_FLUSH_TX_BUFFER = 11;

        public const int canIOCTL_GET_TIMER_SCALE = 12;

        public const int canIOCTL_SET_TXRQ = 13;

        public const int canIOCTL_GET_EVENTHANDLE = 14;

        public const int canIOCTL_SET_BYPASS_MODE = 15;

        public const int canIOCTL_SET_WAKEUP = 16;

        public const int canIOCTL_GET_DRIVERHANDLE = 17;

        public const int canIOCTL_MAP_RXQUEUE = 18;

        public const int canIOCTL_GET_WAKEUP = 19;

        public const int canIOCTL_SET_REPORT_ACCESS_ERRORS = 20;

        public const int canIOCTL_GET_REPORT_ACCESS_ERRORS = 21;

        public const int canIOCTL_CONNECT_TO_VIRTUAL_BUS = 22;

        public const int canIOCTL_DISCONNECT_FROM_VIRTUAL_BUS = 23;

        public const int canIOCTL_SET_USER_IOPORT = 24;

        public const int canIOCTL_GET_USER_IOPORT = 25;

        public const int canIOCTL_SET_BUFFER_WRAPAROUND_MODE = 26;

        public const int canIOCTL_SET_RX_QUEUE_SIZE = 27;

        public const int canIOCTL_SET_USB_THROTTLE = 28;

        public const int canIOCTL_GET_USB_THROTTLE = 29;

        public const int canIOCTL_SET_BUSON_TIME_AUTO_RESET = 30;

        public const int canIOCTL_GET_TXACK = 31;

        public const int canIOCTL_SET_LOCAL_TXECHO = 32;

        public const int canIOCTL_SET_ERROR_FRAMES_REPORTING = 33;

        public const int canIOCTL_GET_CHANNEL_QUALITY = 34;

        public const int canIOCTL_GET_ROUNDTRIP_TIME = 35;

        public const int canIOCTL_GET_BUS_TYPE = 36;

        public const int canIOCTL_GET_DEVNAME_ASCII = 37;

        public const int canIOCTL_GET_TIME_SINCE_LAST_SEEN = 38;

        public const int canIOCTL_TX_INTERVAL = 40;

        public const int canIOCTL_SET_BRLIMIT = 43;

        public const int canIOCTL_SET_THROTTLE_SCALED = 41;

        public const int canIOCTL_GET_THROTTLE_SCALED = 42;

        public const int canIOCTL_RESET_OVERRUN_COUNT = 44;

        public const int canIOCTL_LIN_MODE = 45;

        public const int canIOCTL_SET_LOCAL_TXACK = 46;

        //public const int canNOTIFY_NONE = 0;

        //public const int canNOTIFY_RX = 1;

        //public const int canNOTIFY_TX = 2;

        //public const int canNOTIFY_ERROR = 4;

        //public const int canNOTIFY_STATUS = 8;

        //public const int canNOTIFY_ENVVAR = 16;

        //public const int canNOTIFY_BUSONOFF = 32;

        //public const int canNOTIFY_REMOVED = 64;

        public const int canOBJBUF_AUTO_RESPONSE_RTR_ONLY = 1;

        //public const int canTRANSCEIVER_LINEMODE_NA = 0;

        //public const int canTRANSCEIVER_LINEMODE_SWC_SLEEP = 4;

        //public const int canTRANSCEIVER_LINEMODE_SWC_NORMAL = 5;

        //public const int canTRANSCEIVER_LINEMODE_SWC_FAST = 6;

        //public const int canTRANSCEIVER_LINEMODE_SWC_WAKEUP = 7;

        //public const int canTRANSCEIVER_LINEMODE_SLEEP = 8;

        //public const int canTRANSCEIVER_LINEMODE_NORMAL = 9;

        //public const int canTRANSCEIVER_LINEMODE_STDBY = 10;

        //public const int canTRANSCEIVER_LINEMODE_TT_CAN_H = 11;

        //public const int canTRANSCEIVER_LINEMODE_TT_CAN_L = 12;

        //public const int canTRANSCEIVER_LINEMODE_OEM1 = 13;

        //public const int canTRANSCEIVER_LINEMODE_OEM2 = 14;

        //public const int canTRANSCEIVER_LINEMODE_OEM3 = 15;

        //public const int canTRANSCEIVER_LINEMODE_OEM4 = 16;

        //public const int canTRANSCEIVER_RESNET_NA = 0;

        //public const int canTRANSCEIVER_RESNET_MASTER = 1;

        //public const int canTRANSCEIVER_RESNET_MASTER_STBY = 2;

        //public const int canTRANSCEIVER_RESNET_SLAVE = 3;

        //public const int canTRANSCEIVER_TYPE_UNKNOWN = 0;

        //public const int canTRANSCEIVER_TYPE_251 = 1;

        //public const int canTRANSCEIVER_TYPE_252 = 2;

        //public const int canTRANSCEIVER_TYPE_DNOPTO = 3;

        //public const int canTRANSCEIVER_TYPE_W210 = 4;

        //public const int canTRANSCEIVER_TYPE_SWC_PROTO = 5;

        //public const int canTRANSCEIVER_TYPE_SWC = 6;

        //public const int canTRANSCEIVER_TYPE_EVA = 7;

        //public const int canTRANSCEIVER_TYPE_FIBER = 8;

        //public const int canTRANSCEIVER_TYPE_K251 = 9;

        //public const int canTRANSCEIVER_TYPE_K = 10;

        //public const int canTRANSCEIVER_TYPE_1054_OPTO = 11;

        //public const int canTRANSCEIVER_TYPE_SWC_OPTO = 12;

        //public const int canTRANSCEIVER_TYPE_TT = 13;

        //public const int canTRANSCEIVER_TYPE_1050 = 14;

        //public const int canTRANSCEIVER_TYPE_1050_OPTO = 15;

        //public const int canTRANSCEIVER_TYPE_1041 = 16;

        //public const int canTRANSCEIVER_TYPE_1041_OPTO = 17;

        //public const int canTRANSCEIVER_TYPE_RS485 = 18;

        //public const int canTRANSCEIVER_TYPE_LIN = 19;

        //public const int canTRANSCEIVER_TYPE_KONE = 20;

        //public const int canTRANSCEIVER_TYPE_CANFD = 22;

        //public const int canTRANSCEIVER_TYPE_CANFD_LIN = 24;

        //public const int canTRANSCEIVER_TYPE_LINX_LIN = 64;

        //public const int canTRANSCEIVER_TYPE_LINX_J1708 = 66;

        //public const int canTRANSCEIVER_TYPE_LINX_K = 68;

        //public const int canTRANSCEIVER_TYPE_LINX_SWC = 70;

        //public const int canTRANSCEIVER_TYPE_LINX_LS = 72;

        public const int canTXEDATA_FILE_VERSION = 1;

        public const int canTXEDATA_COMPILER_VERSION = 2;

        public const int canTXEDATA_DATE = 3;

        public const int canTXEDATA_DESCRIPTION = 4;

        public const int canTXEDATA_SOURCE = 5;

        public const int canTXEDATA_SIZE_OF_CODE = 6;

        public const int canTXEDATA_IS_ENCRYPTED = 7;

        public const int canVERSION_DONT_ACCEPT_LATER = 1;

        public const int canVERSION_DONT_ACCEPT_BETAS = 2;

        public const int canVERSION_CANLIB32_VERSION = 0;

        public const int canVERSION_CANLIB32_PRODVER = 1;

        public const int canVERSION_CANLIB32_PRODVER32 = 2;

        public const int canVERSION_CANLIB32_BETA = 3;

        public const int kvBUSTYPE_NONE = 0;

        public const int kvBUSTYPE_PCI = 1;

        public const int kvBUSTYPE_PCMCIA = 2;

        public const int kvBUSTYPE_USB = 3;

        public const int kvBUSTYPE_WLAN = 4;

        public const int kvBUSTYPE_PCI_EXPRESS = 5;

        public const int kvBUSTYPE_ISA = 6;

        public const int kvBUSTYPE_VIRTUAL = 7;

        public const int kvBUSTYPE_PC104_PLUS = 8;

        public const int kvBUSTYPE_LAN = 9;

        public const int kvBUSTYPE_GROUP_VIRTUAL = 1;

        public const int kvBUSTYPE_GROUP_LOCAL = 2;

        public const int kvBUSTYPE_GROUP_REMOTE = 3;

        public const int kvBUSTYPE_GROUP_INTERNAL = 4;

        public const int kvDEVICE_MODE_INTERFACE = 0;

        public const int kvDEVICE_MODE_LOGGER = 1;

        public const int kvENVVAR_TYPE_INT = 1;

        public const int kvENVVAR_TYPE_FLOAT = 2;

        public const int kvENVVAR_TYPE_STRING = 3;

        public const int kvEVENT_TYPE_KEY = 1;

        public const int kvLED_ACTION_ALL_LEDS_ON = 0;

        public const int kvLED_ACTION_ALL_LEDS_OFF = 1;

        public const int kvLED_ACTION_LED_0_ON = 2;

        public const int kvLED_ACTION_LED_0_OFF = 3;

        public const int kvLED_ACTION_LED_1_ON = 4;

        public const int kvLED_ACTION_LED_1_OFF = 5;

        public const int kvLED_ACTION_LED_2_ON = 6;

        public const int kvLED_ACTION_LED_2_OFF = 7;

        public const int kvLED_ACTION_LED_3_ON = 8;

        public const int kvLED_ACTION_LED_3_OFF = 9;

        public const int kvLED_ACTION_LED_4_ON = 10;

        public const int kvLED_ACTION_LED_4_OFF = 11;

        public const int kvLED_ACTION_LED_5_ON = 12;

        public const int kvLED_ACTION_LED_5_OFF = 13;

        public const int kvLED_ACTION_LED_6_ON = 14;

        public const int kvLED_ACTION_LED_6_OFF = 15;

        public const int kvLED_ACTION_LED_7_ON = 16;

        public const int kvLED_ACTION_LED_7_OFF = 17;

        public const int kvLED_ACTION_LED_8_ON = 18;

        public const int kvLED_ACTION_LED_8_OFF = 19;

        public const int kvLED_ACTION_LED_9_ON = 20;

        public const int kvLED_ACTION_LED_9_OFF = 21;

        public const int kvLED_ACTION_LED_10_ON = 22;

        public const int kvLED_ACTION_LED_10_OFF = 23;

        public const int kvLED_ACTION_LED_11_ON = 24;

        public const int kvLED_ACTION_LED_11_OFF = 25;

        public const int kvSCRIPT_STOP_NORMAL = 0;

        public const int kvSCRIPT_STOP_FORCED = -9;

        public const int kvSCRIPT_REQUEST_TEXT_UNSUBSCRIBE = 1;

        public const int kvSCRIPT_REQUEST_TEXT_SUBSCRIBE = 2;

        public const int kvSCRIPT_REQUEST_TEXT_ALL_SLOTS = 255;

        public const int kvSCRIPT_STATUS_LOADED = 1;

        public const int kvSCRIPT_STATUS_RUNNING = 2;

        //public const int canINVALID_HANDLE = -1;

        private const int WM_USER = 1024;

        private static int _refCount = 0;
        private static readonly object _initLock = new object();

        //public const int WM__CANLIB = 17378;
        const string DLL_NAME = ".\\HwDependency\\Kvaser\\Generic\\Win\\x64\\canlib32.dll";

        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canSetAcceptanceFilter(int hnd, uint code, uint mask, int is_extended);

        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canGetNumberOfChannels(out int channelCount);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canBusOff(int hnd);

        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canBusOn(int hnd);

        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canClose(int hnd);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canFlushReceiveQueue(int hnd);

        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canFlushTransmitQueue(int hnd);

        /// <summary>
        /// 整个电脑只执行一次kvaser 库文件的卸载
        /// </summary>
        [DllImport(DLL_NAME)]
        public static extern void canInitializeLibrary();
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canIoCtl(int hnd, uint func, IntPtr buf, uint buflen);

        [DllImport(DLL_NAME)]
        public static extern int canOpenChannel(int channel, int flags);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canParamSetBusParams(int index, int bitrate, uint tseg1, uint tseg2, uint sjw, uint noSamp);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canRead(int hnde, out int id, IntPtr msg, out uint dlc, out uint flag, out uint time);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canReadErrorCounters(int hnd, out uint txErr, out uint rxERR, out uint ovErr);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canIoCtl(int hnd, uint func, ref uint ctilIO, uint buflen);

        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canSetBitrate(int hnd, int bitrate);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canSetBusParamsFd(int hnd, int freq_brs, uint tseg1_brs, uint tseg2_brs, uint sjw_brs);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canWrite(int hnd, int id, IntPtr msg, uint dlc, uint flag);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canWriteWait(int hnd, int id, IntPtr msg, uint dlc, uint flag, long timeout);
        /// <summary>
        /// 整个电脑只执行一次kvaser 库文件的卸载
        /// </summary>
        /// <returns></returns>
        [DllImport(DLL_NAME)]
        private static extern Canlib.canStatus canUnloadLibrary();
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canSetBusParams(int hnd, int freq, uint tseg1, uint tseg2, uint sjw, uint noSamp, uint syncmode);

        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canGetChannelData(int channel, int item, IntPtr buffer, UIntPtr bufsize);

        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canSetBusParamsTq(int hnd, kvBusParamsTq nomonal);
        [DllImport(DLL_NAME)]
        public static extern Canlib.canStatus canSetBusParamsFdTq(int hnd, kvBusParamsTq nominal, kvBusParamsTq data);

        public static void InitializeLibrary()
        {
            lock (_initLock)
            {
                canInitializeLibrary();
                _refCount++;
            }
        }

        public static void UnloadLibrary()
        {
            lock (_initLock)
            {
                if (_refCount > 0)
                {
                    _refCount--;
                    if (_refCount == 0)
                    {
                        canUnloadLibrary();
                    }
                }
            }
        }

        public static canStatus canGetChannelData(int channel, int item, out object buffer)
        {
            return canGetChannelOrHandleData(KvaserCanLibDLL.canGetChannelData, channel, item, out buffer);
        }
        private unsafe static canStatus canGetChannelOrHandleData(CanGetChannelOrHandleDataDelegate executor, int channel, int item, out object buffer)
        {
            canStatus canStatus = canStatus.canERR_NOT_IMPLEMENTED;
            buffer = null;
            switch (item)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 16:
                case 17:
                case 18:
                case 19:
                case 20:
                case 28:
                case 29:
                case 30:
                case 32:
                case 33:
                case 37:
                case 38:
                case 40:
                case 41:
                case 42:
                    {
                        uint num2 = default(uint);
                        canStatus = executor(channel, item, (IntPtr)(&num2), (UIntPtr)4uL);
                        if (canStatus == canStatus.canOK)
                        {
                            buffer = num2;
                        }

                        break;
                    }
                case 47:
                    {
                        ulong[] array = new ulong[2];
                        fixed (ulong* ptr2 = array)
                        {
                            canStatus = executor(channel, item, (IntPtr)ptr2, (UIntPtr)16uL);
                            if (canStatus == canStatus.canOK)
                            {
                                buffer = array;
                            }
                        }

                        break;
                    }
                case 7:
                case 8:
                case 9:
                case 10:
                    {
                        long num = default(long);
                        canStatus = executor(channel, item, (IntPtr)(&num), (UIntPtr)8uL);
                        if (canStatus == canStatus.canOK)
                        {
                            buffer = num;
                        }

                        break;
                    }
                case 11:
                case 12:
                case 44:
                    {
                        ulong num3 = default(ulong);
                        canStatus = executor(channel, item, (IntPtr)(&num3), (UIntPtr)8uL);
                        if (canStatus == canStatus.canOK)
                        {
                            buffer = num3;
                        }

                        break;
                    }
                case 13:
                case 24:
                case 26:
                case 27:
                case 31:
                case 34:
                case 35:
                case 36:
                case 39:
                    {
                        byte[] array2 = new byte[(uint)MAX_STRING_SIZE];
                        fixed (byte* ptr4 = array2)
                        {
                            canStatus = executor(channel, item, (IntPtr)ptr4, MAX_STRING_SIZE);
                        }

                        if (canStatus == canStatus.canOK)
                        {
                            buffer = GetUTF8String(array2);
                        }

                        break;
                    }
                case 23:
                case 25:
                    fixed (ushort* ptr3 = new ushort[(uint)MAX_STRING_SIZE])
                    {
                        canStatus = executor(channel, item, (IntPtr)ptr3, (UIntPtr)(ulong)(int)(2 * (uint)MAX_STRING_SIZE));
                        if (canStatus == canStatus.canOK)
                        {
                            buffer = new string((char*)ptr3);
                        }
                    }

                    break;
                case 14:
                case 15:
                case 21:
                case 22:
                    {
                        ushort* ptr = stackalloc ushort[4];
                        canStatus = executor(channel, item, (IntPtr)ptr, (UIntPtr)8uL);
                        canChannelDataVersion canChannelDataVersion = new canChannelDataVersion
                        {
                            Version1 = *ptr,
                            Version2 = ptr[1],
                            Version3 = ptr[2],
                            Version4 = ptr[3]
                        };
                        if (canStatus == canStatus.canOK)
                        {
                            buffer = (int[])canChannelDataVersion;
                        }

                        break;
                    }
                case 43:
                    {
                        canChannelDataHwStatus canChannelDataHwStatus = default(canChannelDataHwStatus);
                        canStatus = executor(channel, item, (IntPtr)(&canChannelDataHwStatus), (UIntPtr)(ulong)sizeof(canChannelDataHwStatus));
                        if (canStatus == canStatus.canOK)
                        {
                            buffer = canChannelDataHwStatus;
                        }

                        break;
                    }
                case 45:
                    {
                        kvBusParamLimits kvBusParamLimits = default(kvBusParamLimits);
                        canStatus = executor(channel, item, (IntPtr)(&kvBusParamLimits), (UIntPtr)(ulong)sizeof(kvBusParamLimits));
                        if (canStatus == canStatus.canOK)
                        {
                            buffer = kvBusParamLimits;
                        }

                        break;
                    }
                case 46:
                    {
                        kvClockInfo kvClockInfo = default(kvClockInfo);
                        canStatus = executor(channel, item, (IntPtr)(&kvClockInfo), (UIntPtr)(ulong)sizeof(kvClockInfo));
                        if (canStatus == canStatus.canOK)
                        {
                            buffer = kvClockInfo;
                        }

                        break;
                    }
                default:
                    canStatus = canStatus.canERR_PARAM;
                    break;
            }

            return canStatus;
        }
        //
        // 摘要:
        //     Convert byte[] contents to a string object using the utf-8 decoder.
        public static string GetUTF8String(byte[] bytes)
        {
            int num = Array.IndexOf(bytes, (byte)0);
            return Encoding.UTF8.GetString(bytes, 0, (num == -1) ? bytes.Length : num);
        }

        public static canStatus SetTimeScale(int hnd, uint us, uint buflen)
        {
            unsafe
            {
                return  KvaserCanLibDLL.canIoCtl(hnd, canIOCTL_SET_TIMER_SCALE, (IntPtr)(&us), buflen);
            }
        }
    }
}