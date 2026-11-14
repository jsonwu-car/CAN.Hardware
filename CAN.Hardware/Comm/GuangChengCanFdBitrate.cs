namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 广成科技 USBCANFD 仲裁段波特率(INIT_CONFIG.NominalBitRateSelect)。
    /// 枚举值必须与厂商头文件 BAUD_RATE 的序号严格一致。
    /// </summary>
    public enum GuangChengCanFdNominalBitrate
    {
        _1M = 0,
        _800K = 1,
        _500K = 2,
        _400K = 3,
        _250K = 4,
        _200K = 5,
        _125K = 6,
        _100K = 7,
        _80K = 8,
        _62500 = 9,
        _50K = 10,
        _40K = 11,
        _25K = 12,
        _20K = 13,
        _10K = 14,
        _5K = 15,
    }

    /// <summary>
    /// 广成科技 USBCANFD 数据段波特率(INIT_CONFIG.DataBitRateSelect)。
    /// 枚举值必须与厂商头文件 DATA_RATE 的序号严格一致。
    /// </summary>
    public enum GuangChengCanFdDataBitrate
    {
        _5M = 0,
        _4M = 1,
        _2M = 2,
        _1M = 3,
        _800K = 4,
        _500K = 5,
        _400K = 6,
        _250K = 7,
        _200K = 8,
        _125K = 9,
        _100K = 10,
        _80K = 11,
        _62500 = 12,
        _50K = 13,
        _40K = 14,
        _25K = 15,
        _20K = 16,
        _10K = 17,
        _5K = 18,
    }
}
