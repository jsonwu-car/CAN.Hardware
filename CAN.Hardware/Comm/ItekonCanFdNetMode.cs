namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技网络型 CANFD 设备的 TCP 工作模式。
    /// 参考官方 Qt Net 示例:0=UDP,1=TCP Server,2=TCP Client。
    /// </summary>
    public enum ItekonCanFdNetMode : byte
    {
        Udp = 0,
        TcpServer = 1,
        TcpClient = 2,
    }
}
