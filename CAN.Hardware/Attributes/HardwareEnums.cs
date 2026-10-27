namespace CAN.Hardware.Attributes
{
    /// <summary>
    /// 硬件厂商
    /// </summary>
    public enum HardwareManufacturer
    {
        Kvaser,
        Peak,
        Tosun,
        Zlg,
        Vector,
        Ni,
    }

    /// <summary>
    /// 总线通信协议
    /// </summary>
    public enum CommProtocol
    {
        CAN,
        CANFD,
        LIN,
    }

    /// <summary>
    /// Kvaser 设备类型
    /// </summary>
    public enum KvaserDeviceType
    {
        Generic,
    }

    /// <summary>
    /// Peak 设备类型
    /// </summary>
    public enum PeakDeviceType
    {
        Generic,
    }

    /// <summary>
    /// Tosun(同星) 设备类型
    /// </summary>
    public enum TosunDeviceType
    {
        Generic,
    }

    /// <summary>
    /// ZLG(周立功) 设备类型
    /// </summary>
    public enum ZlgDeviceType
    {
        USBCAN,
        USBCANFD200U
    }

    /// <summary>
    /// Vector 设备类型
    /// </summary>
    public enum VectorDeviceType
    {
        Generic,
    }

    /// <summary>
    /// NI(National Instruments) 设备类型
    /// </summary>
    public enum NiDeviceType
    {
        Generic,
    }
}
