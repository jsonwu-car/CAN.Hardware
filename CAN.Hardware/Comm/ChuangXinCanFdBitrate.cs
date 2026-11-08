namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 创芯科技 USBCANFD 数据段波特率。
    /// ControlCANFD(ZCAN) 直接使用 bps 数值,枚举仅用于约束可选范围。
    /// </summary>
    public enum ChuangXinCanFdBitrate
    {
        _100K,
        _125K,
        _250K,
        _500K,
        _800K,
        _1M,
        _2M,
        _4M,
        _5M,
    }
}
