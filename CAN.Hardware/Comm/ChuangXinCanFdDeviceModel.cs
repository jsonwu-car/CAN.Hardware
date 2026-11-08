namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 创芯科技 USBCANFD 分析仪型号,枚举值即 ControlCANFD(ZCAN) 的 device_type。
    /// 参考《设备型号对应的资料包.txt》:USBCANFD 分析仪使用 CANFD 资料包(ControlCANFD.dll)。
    /// </summary>
    public enum ChuangXinCanFdDeviceModel : uint
    {
        /// <summary>USBCANFD-200U(2 通道)</summary>
        USBCANFD_200U = 41,

        /// <summary>USBCANFD-100U(1 通道)</summary>
        USBCANFD_100U = 42,

        /// <summary>USBCANFD-MINI(1 通道)</summary>
        USBCANFD_MINI = 43,
    }
}
