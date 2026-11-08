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
        Esd,
        Ni,
        Etas,
        Intrepid,
        EbEl,
        ChuangXin,
        Toomoss,
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
    /// 创芯科技(ChuangXin) 设备类型。
    /// 经典 CAN 与 CANFD 使用两套不同的库:
    /// USBCAN  -> ControlCAN.dll(VCI_ API),对应 USBCAN-2A/2C、CANalyst-II 至尊版/顶配/顶配Pro、OBD 版;
    /// USBCANFD-> ControlCANFD.dll(ZCAN_ API),对应 USBCANFD 分析仪(200U/100U/MINI)。
    /// </summary>
    public enum ChuangXinDeviceType
    {
        USBCAN,
        USBCANFD,
    }

    /// <summary>
    /// 图莫斯(Toomoss) USB2XXX 设备类型。
    /// USB2XXX 系列(USB2CAN / USB2CANFD / USB2XXX 多功能适配器)统一由 USB2XXX.dll 驱动,
    /// CAN 与 CANFD 只是同一设备上的不同协议通路,因此统一使用 Generic。
    /// </summary>
    public enum ToomossDeviceType
    {
        Generic,
    }

    /// <summary>
    /// Vector 设备类型
    /// </summary>
    public enum VectorDeviceType
    {
        Generic,
    }

    /// <summary>
    /// ESD(esd electronics) 设备类型
    /// </summary>
    public enum EsdDeviceType
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
    /// ETAS 设备类型。
    /// ETAS 走 BOA V2 驱动 + CSI 协议树枚举,不按型号区分(Generic 即全部):
    /// 凡 BOA V2 驱动支持、CSI 树中暴露 CAN 控制器端口的硬件均可使用,常见型号如
    /// ES581.4(USB-CAN)、ES582.1/ES582.2/ES584.1(USB CAN FD)、
    /// ES592.1/ES593-D/ES595.1(ES590 系列 CAN FD)、ES523/ES525(ES500 CAN 模块,经 ES600 网络模块级联)。
    /// </summary>
    public enum EtasDeviceType
    {

        Generic,
    }

    /// <summary>
    /// Intrepid(英特佩斯) 设备类型。
    /// 与 ETAS/NI 一样不按型号区分(Generic 即全部):凡 neoVI API(icsneo40.dll)
    /// 支持的设备均可使用,常见型号如 neoVI FIRE2/FIRE3、neoVI RED2、ValueCAN4 系列、
    /// RAD-Galaxy/Gigastar 等(取设备的高速 CAN 通道 HSCAN1~HSCAN7)。
    /// </summary>
    public enum IntrepidDeviceType
    {
        Generic,
    }

    /// <summary>
    /// EbEl(Eberspächer Electronics 埃贝赫电子) 设备类型。
    /// 不按型号区分(Generic 即全部):凡 FlexCard 驱动(fcbase.dll)支持、
    /// 带 CAN 控制器的硬件均可使用,如 FlexCard Cyclone II 系列、FlexCard USB、FlexXCon 系列。
    /// 注意:该 API 只有经典 CAN,无 CAN FD。
    /// </summary>
    public enum EbElDeviceType
    {
        Generic,
    }
}
