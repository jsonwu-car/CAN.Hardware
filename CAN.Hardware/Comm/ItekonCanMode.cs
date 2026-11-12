namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技经典 CAN(ControlCAN) 工作模式。
    /// 对应 VCI_INIT_CONFIG.Mode:0=正常,1=只听。
    /// </summary>
    public enum ItekonCanMode
    {
        Normal = 0,
        ListenOnly = 1,
    }
}
