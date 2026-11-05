namespace CAN.Hardware.Comm
{
    /// <summary>LIN节点模式(通用):主节点/从节点/监听节点,各厂商wrapper负责映射到各自的SDK枚举</summary>
    public enum LinNodeMode : byte
    {
        Master = 0,
        Slave = 1,
        Monitor = 2,
    }
}
