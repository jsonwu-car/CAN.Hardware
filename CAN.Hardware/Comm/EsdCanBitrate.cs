namespace CAN.Hardware.Comm
{
    /// <summary>
    /// ESD CAN 标准波特率,枚举值即实际波特率(bps),
    /// 经 EsdBaudMode.USER_BAUDRATE_NUM|bps 传入 canSetBaudrate。
    /// </summary>
    public enum EsdCanBitrate : uint
    {
        esdBAUD_10K = 10000,
        esdBAUD_20K = 20000,
        esdBAUD_50K = 50000,
        esdBAUD_100K = 100000,
        esdBAUD_125K = 125000,
        esdBAUD_250K = 250000,
        esdBAUD_500K = 500000,
        esdBAUD_800K = 800000,
        esdBAUD_1M = 1000000,
    }

    /// <summary>
    /// ESD CAN FD 数据段波特率,枚举值即实际波特率(bps),
    /// 经 canSetBaudrateX(NUM 模式) 传入数据段。
    /// </summary>
    public enum EsdCanFdBitrate : uint
    {
        esdBAUD_500K = 500000,
        esdBAUD_1M = 1000000,
        esdBAUD_2M = 2000000,
        esdBAUD_4M = 4000000,
        esdBAUD_5M = 5000000,
        esdBAUD_8M = 8000000,
        esdBAUD_10M = 10000000,
    }
}
