namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技 CANFD 数据段波特率。
    /// iTekCANFD 直接使用寄存器值,枚举与寄存器的对应关系见
    /// <see cref="BaseLibDll.ItekonCanFdLibDLL.BuildDataTiming"/>。
    /// </summary>
    public enum ItekonCanFdDataBitrate
    {
        _100K,
        _125K,
        _200K,
        _250K,
        _400K,
        _500K,
        _800K,
        _1M,
        _2M,
        _3M,
        _4M,
        _5M,
        _6M,
        _8M,
        _10M,
        _12M,
    }
}
