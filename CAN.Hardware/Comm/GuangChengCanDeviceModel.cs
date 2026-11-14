namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 广成科技(GuangCheng) USBCAN 经典 CAN 设备类型。
    /// ECanVci 接口库只按 3/4 区分单通道与双通道,型号差异由硬件自身决定。
    /// </summary>
    public enum GuangChengCanDeviceModel
    {
        /// <summary>USBCAN-I(单通道,设备类型号 3)</summary>
        USBCAN_I = 3,

        /// <summary>USBCAN-II(双通道,设备类型号 4)</summary>
        USBCAN_II = 4,
    }
}
