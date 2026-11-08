namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 图莫斯经典 CAN 工作模式(CAN_INIT_CONFIG.CAN_Mode 低 7 位)。
    /// </summary>
    public enum ToomossCanMode
    {
        Normal = 0,
        LoopBack = 1,
        Silent = 2,
        SilentLoopBack = 3,
    }

    /// <summary>
    /// 图莫斯 CANFD 工作模式(CANFD_INIT_CONFIG.Mode)。
    /// </summary>
    public enum ToomossCanFdMode
    {
        Normal = 0,
        SelfReceive = 1,
    }

    /// <summary>
    /// 图莫斯经典 CAN 过滤器模式(CAN_FILTER_CONFIG.FilterMode)。
    /// </summary>
    public enum ToomossCanFilterMode
    {
        Mask = 0,
        List = 1,
    }
}
