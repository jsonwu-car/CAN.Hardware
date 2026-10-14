namespace CAN.Hardware.Attributes
{
    /// <summary>
    /// 硬件厂商
    /// </summary>
    public enum HardwareManufacturer
    {
        Kvaser,
        Peak,
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
}
