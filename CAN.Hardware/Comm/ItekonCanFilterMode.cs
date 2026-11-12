namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技经典 CAN(ControlCAN) 滤波方式。
    /// 对应 VCI_INIT_CONFIG.Filter:0=双滤波,1=单滤波。
    /// 滤波 ID 范围由 AccCode/AccMask 决定。
    /// </summary>
    public enum ItekonCanFilterMode : byte
    {
        Dual = 0,
        Single = 1,
    }
}
