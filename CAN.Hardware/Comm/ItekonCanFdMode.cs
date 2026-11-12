namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技 CANFD 工作模式(workMode):0=正常,1=只听。
    /// </summary>
    public enum ItekonCanFdMode : byte
    {
        Normal = 0,
        ListenOnly = 1,
    }
}
