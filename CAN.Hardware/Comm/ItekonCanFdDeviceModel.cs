namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技 CANFD 设备型号,枚举值即 iTekCANFD(iTek_ API) 的 Device_Type。
    /// 参考《iTekCANFD 函数库使用手册》表 4-1。
    /// </summary>
    public enum ItekonCanFdDeviceModel : ushort
    {
        /// <summary>USBCANFD-X100 / PCIeCANFD-X100 / MiniPCIeCANFD-X100 / M.2CANFD-X100</summary>
        USBCANFD_X100 = 1,

        /// <summary>USBCANFD-X200 / PCIeCANFD-X200 / MiniPCIeCANFD-X200 / M.2CANFD-X200</summary>
        USBCANFD_X200 = 2,

        /// <summary>NETCANFD-L100(手册表 4-1 同时列 WifiCANFD-W100=3;头文件/示例 W100=7)</summary>
        NETCANFD_L100 = 3,

        /// <summary>NETCANFD-L200(手册表 4-1 同时列 WifiCANFD-W200=4;头文件/示例 W200=8)</summary>
        NETCANFD_L200 = 4,

        /// <summary>NETCAN-1(经典 CAN,头文件 iTek_NETCAN_1;CANFD 手册表 4-1 未列)</summary>
        NETCAN_1 = 5,

        /// <summary>NETCAN-2(经典 CAN,头文件 iTek_NETCAN_2;CANFD 手册表 4-1 未列)</summary>
        NETCAN_2 = 6,

        /// <summary>WifiCANFD-W100(头文件 iTek_WIFICANFD_1;手册表 4-1 写 3)</summary>
        WIFICANFD_1 = 7,

        /// <summary>WifiCANFD-W200(头文件 iTek_WIFICANFD_2;手册表 4-1 写 4)</summary>
        WIFICANFD_2 = 8,

        /// <summary>WifiCAN-1(经典 CAN,头文件 iTek_WIFICAN_1;CANFD 手册表 4-1 未列)</summary>
        WIFICAN_1 = 9,

        /// <summary>WifiCAN-2(经典 CAN,头文件 iTek_WIFICAN_2;CANFD 手册表 4-1 未列)</summary>
        WIFICAN_2 = 10,

        /// <summary>USBCANFD-X400 / PCIeCANFD-X400 / MiniPCIeCANFD-X400 / M.2CANFD-X400</summary>
        USBCANFD_X400 = 11,

        /// <summary>USBCANFD-X800</summary>
        USBCANFD_X800 = 12,

        /// <summary>NETCANFD-L400</summary>
        NETCANFD_L400 = 13,

        /// <summary>NETCANFD-L800</summary>
        NETCANFD_L800 = 14,
    }
}
