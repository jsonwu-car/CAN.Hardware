namespace CAN.Hardware.Comm
{
    /// <summary>
    /// NI-XNET CAN 波特率,枚举值即波特率 bps 值(直接写入 Session:Intf:Baud Rate 属性)
    /// </summary>
    public enum NiCanBitrate : ulong
    {
        niBAUD_125K = 125000,
        niBAUD_250K = 250000,
        niBAUD_500K = 500000,
        niBAUD_1M = 1000000,
    }

    /// <summary>
    /// NI-XNET CAN FD 数据段波特率,枚举值即波特率 bps 值(直接写入 Session:Intf:CAN FD Baud Rate 属性)
    /// </summary>
    public enum NiCanFdBitrate : ulong
    {
        niFDBAUD_500K = 500000,
        niFDBAUD_1M = 1000000,
        niFDBAUD_2M = 2000000,
        niFDBAUD_4M = 4000000,
        niFDBAUD_5M = 5000000,
        niFDBAUD_8M = 8000000,
    }
}
