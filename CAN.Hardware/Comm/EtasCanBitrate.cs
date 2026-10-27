namespace CAN.Hardware.Comm
{
    /// <summary>
    /// ETAS BOA CAN 波特率,枚举值即波特率 Hz 值(直接填入 OCI_CANConfiguration.baudrate)
    /// </summary>
    public enum EtasCanBitrate : uint
    {
        etasBAUD_125K = 125000,
        etasBAUD_250K = 250000,
        etasBAUD_500K = 500000,
        etasBAUD_1M = 1000000,
    }

    /// <summary>
    /// ETAS BOA CAN FD 数据段波特率,枚举值即波特率 Hz 值(直接填入 OCI_CANFDConfiguration.bitRate)
    /// </summary>
    public enum EtasCanFdBitrate : uint
    {
        etasFDBAUD_500K = 500000,
        etasFDBAUD_1M = 1000000,
        etasFDBAUD_2M = 2000000,
        etasFDBAUD_4M = 4000000,
        etasFDBAUD_5M = 5000000,
        etasFDBAUD_8M = 8000000,
    }
}
