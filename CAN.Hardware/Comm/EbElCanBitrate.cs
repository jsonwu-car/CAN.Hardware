namespace CAN.Hardware.Comm
{
    /// <summary>
    /// EbEl(Eberspächer Electronics FlexCard) CAN 波特率,枚举值即 bps。
    /// fcbase 无数值波特率接口,Open 时按枚举查位时序表
    /// (BaudRatePrescaler/SJW/TS1/TS2,见 EbElCanWrapper.BuildCcConfig)。
    /// </summary>
    public enum EbElCanBitrate
    {
        fcBAUD_10K = 10000,
        fcBAUD_20K = 20000,
        fcBAUD_50K = 50000,
        fcBAUD_100K = 100000,
        fcBAUD_125K = 125000,
        fcBAUD_250K = 250000,
        fcBAUD_500K = 500000,
        fcBAUD_800K = 800000,
        fcBAUD_1M = 1000000,
    }
}
