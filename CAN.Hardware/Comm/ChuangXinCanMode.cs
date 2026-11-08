namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 创芯科技经典 CAN(ControlCAN) 工作模式。
    /// 对应 VCI_INIT_CONFIG.Mode:0=正常,1=只听,2=自测。
    /// </summary>
    public enum ChuangXinCanMode
    {
        Normal = 0,
        ListenOnly = 1,
        SelfTest = 2,
    }

    /// <summary>
    /// 创芯科技 USBCANFD(ControlCANFD) 工作模式。
    /// 对应 ZCAN_CHANNEL_INIT_CONFIG.canfd.mode:0=正常,1=只听。
    /// </summary>
    public enum ChuangXinCanFdMode
    {
        Normal = 0,
        ListenOnly = 1,
    }
}
