namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技 CANFD 发送方式(send_type):0=正常发送,1=自发自收。
    /// </summary>
    public enum ItekonCanFdSendType : ushort
    {
        Normal = 0,
        SelfSendReceive = 1,
    }
}
