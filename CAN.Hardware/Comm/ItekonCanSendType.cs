namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技经典 CAN(ControlCAN) 发送帧类型。
    /// 对应 VCI_CAN_OBJ.SendType:0=正常发送,1=单次发送,2=自发自收,3=单次自发自收。
    /// </summary>
    public enum ItekonCanSendType : byte
    {
        Normal = 0,
        SingleShot = 1,
        SelfSendReceive = 2,
        SingleShotSelfSendReceive = 3,
    }
}
