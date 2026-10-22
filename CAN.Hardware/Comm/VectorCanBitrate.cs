namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Vector CAN 标准波特率,枚举值即实际波特率(bps),直接传入 xlCanSetChannelBitrate。
    /// </summary>
    public enum VectorCanBitrate : uint
    {
        canBITRATE_10K = 10000,
        canBITRATE_20K = 20000,
        canBITRATE_50K = 50000,
        canBITRATE_100K = 100000,
        canBITRATE_125K = 125000,
        canBITRATE_250K = 250000,
        canBITRATE_500K = 500000,
        canBITRATE_800K = 800000,
        canBITRATE_1M = 1000000,
    }
}
