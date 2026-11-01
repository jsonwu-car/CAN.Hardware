namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Intrepid CAN 波特率,枚举值即波特率 bps 值(直接传给 icsneoSetBitRate)。
    /// </summary>
    public enum IntrepidCanBitrate
    {
        icsBAUD_10K = 10000,
        icsBAUD_20K = 20000,
        icsBAUD_50K = 50000,
        icsBAUD_100K = 100000,
        icsBAUD_125K = 125000,
        icsBAUD_250K = 250000,
        icsBAUD_500K = 500000,
        icsBAUD_800K = 800000,
        icsBAUD_1M = 1000000,
    }

    /// <summary>
    /// Intrepid CAN FD 数据段波特率,枚举值即波特率 bps 值(直接传给 icsneoSetFDBitRate)。
    /// </summary>
    public enum IntrepidCanFdBitrate
    {
        icsFDBAUD_500K = 500000,
        icsFDBAUD_1M = 1000000,
        icsFDBAUD_2M = 2000000,
        icsFDBAUD_4M = 4000000,
        icsFDBAUD_5M = 5000000,
        icsFDBAUD_8M = 8000000,
        icsFDBAUD_10M = 10000000,
    }
}
