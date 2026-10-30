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
        Etas,
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
    /// ETAS 设备类型。
    /// ETAS 走 BOA V2 驱动 + CSI 协议树枚举,不按型号区分(Generic 即全部):
    /// 凡 BOA V2 驱动支持、CSI 树中暴露 CAN 控制器端口的硬件均可使用,常见型号如
    /// ES581.4(USB-CAN)、ES582.1/ES582.2/ES584.1(USB CAN FD)、
    /// ES592.1/ES593-D/ES595.1(ES590 系列 CAN FD)、ES523/ES525(ES500 CAN 模块,经 ES600 网络模块级联)。
    /// </summary>
    public enum EtasDeviceType
    {
        /// <summary>全部 ETAS BOA V2 CAN/CANFD 硬件通用</summary>
        Generic,
    }
}
