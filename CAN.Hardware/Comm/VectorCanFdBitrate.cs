namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Vector CAN FD 数据段波特率,枚举值即实际波特率(bps)。
    /// </summary>
    public enum VectorCanFdBitrate : uint
    {
        canFD_BITRATE_500K = 500000,
        canFD_BITRATE_1M = 1000000,
        canFD_BITRATE_2M = 2000000,
        canFD_BITRATE_4M = 4000000,
        canFD_BITRATE_5M = 5000000,
        canFD_BITRATE_8M = 8000000,
    }
}
