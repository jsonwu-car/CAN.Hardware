namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技经典 CAN 设备型号,枚举值即 ControlCAN(VCI) 的 DeviceType。
    /// 参考《CAN-bus 接口函数库使用手册 V2.0.1》1.1 节;
    /// 另保留资料包头文件中与 ZLG 兼容的型号定义。
    /// </summary>
    public enum ItekonCanDeviceModel : uint
    {
        /// <summary>PCICAN-9810(VCI_PCI9810)</summary>
        PCI9810 = 2,

        /// <summary>USBCAN-I/I+、iCANEC-I(VCI_USBCAN1)</summary>
        USBCAN1 = 3,

        /// <summary>USBCAN-2I/2I+(VCI_USBCAN2),常用默认值</summary>
        USBCAN2 = 4,

        /// <summary>PCICAN-9820 / PC104+9820 / CPCI-9820(VCI_PCI9820)</summary>
        PCI9820 = 5,

        /// <summary>CAN232(VCI_CAN232),资料包头文件兼容定义</summary>
        CAN232 = 6,

        /// <summary>CANLITE(VCI_CANLITE),资料包头文件兼容定义</summary>
        CANLITE = 8,

        /// <summary>CANET UDP(VCI_CANETUDP),资料包头文件兼容定义</summary>
        CANETUDP = 12,

        /// <summary>PCICAN-9840(VCI_PCI9840)</summary>
        PCI9840 = 14,

        /// <summary>CANET TCP(VCI_CANETTCP),资料包头文件兼容定义</summary>
        CANETTCP = 17,

        /// <summary>PCI-5010-U(VCI_PCI5010U),资料包头文件兼容定义</summary>
        PCI5010U = 19,

        /// <summary>USBCAN-E-U(VCI_USBCAN_E_U),资料包头文件兼容定义</summary>
        USBCAN_E_U = 20,

        /// <summary>USBCAN-2E-U(VCI_USBCAN_2E_U),资料包头文件兼容定义</summary>
        USBCAN_2E_U = 21,

        /// <summary>PCI-5020-U(VCI_PCI5020U),资料包头文件兼容定义</summary>
        PCI5020U = 22,

        /// <summary>PCIeCAN-9221(VCI_PCIE9221)</summary>
        PCIE9221 = 24,

        /// <summary>WIFICAN TCP(VCI_WIFICAN_TCP),资料包头文件兼容定义</summary>
        WIFICAN_TCP = 25,

        /// <summary>WIFICAN UDP(VCI_WIFICAN_UDP),资料包头文件兼容定义</summary>
        WIFICAN_UDP = 26,
    }
}
