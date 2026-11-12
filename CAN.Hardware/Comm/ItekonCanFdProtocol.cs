namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技 CANFD 通道协议类型(can_type)。
    /// 0=CAN,1=CANFD,2=CANFD 加速;通道按所选类型初始化后仍可收发经典 CAN 帧。
    /// </summary>
    public enum ItekonCanFdProtocol : byte
    {
        CAN = 0,
        CANFD = 1,
        CANFD_Accelerated = 2,
    }
}
