namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 创芯科技经典 CAN 设备型号,枚举值即 ControlCAN(VCI) 的 DeviceType。
    /// 参考《设备型号对应的资料包.txt》:
    /// USBCAN-2A、USBCAN-2C、CANalyst-II 至尊版(红/银)、至尊版带 OBD 转接头、
    /// CANalyst-II 顶配版、顶配版 Pro(OBDII 专用)均使用 ControlCAN.dll,
    /// 创芯经典型号统一使用 USBCAN2(4,双通道);USBCAN1/E_U/2E_U 为兼容 ZLG 的 DeviceType。
    /// </summary>
    public enum ChuangXinCanDeviceModel : uint
    {
        /// <summary>兼容 ZLG USBCAN1(单通道, VCI_USBCAN1)</summary>
        USBCAN1 = 3,

        /// <summary>双通道经典 CAN 设备(VCI_USBCAN2),USBCAN-2A/2C、CANalyst-II 系列默认值</summary>
        USBCAN2 = 4,

        /// <summary>兼容 ZLG USBCAN-E-U 型号(VCI_USBCAN_E_U)</summary>
        USBCAN_E_U = 20,

        /// <summary>兼容 ZLG USBCAN-2E-U 型号(VCI_USBCAN_2E_U)</summary>
        USBCAN_2E_U = 21,
    }
}
