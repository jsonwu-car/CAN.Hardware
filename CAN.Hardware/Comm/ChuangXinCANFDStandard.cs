namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 创芯科技 USBCANFD 的 CAN FD 标准(0=ISO,1=BOSCH 非 ISO)。
    /// 与 ControlCANFD 的 ZCAN_SetCANFDStandard 取值一致。
    /// </summary>
    public enum ChuangXinCANFDStandard
    {
        CANFD_ISO,
        CANFD_BOSCH,
    }
}
