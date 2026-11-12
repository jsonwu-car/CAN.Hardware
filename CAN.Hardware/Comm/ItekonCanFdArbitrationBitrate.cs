namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技 CANFD 仲裁段波特率。
    /// iTekCANFD 直接使用寄存器值,枚举与寄存器的对应关系见
    /// <see cref="BaseLibDll.ItekonCanFdLibDLL.BuildArbitrationTiming"/>。
    /// </summary>
    public enum ItekonCanFdArbitrationBitrate
    {
        _5K,
        _10K,
        _20K,
        _40K,
        _50K,
        _80K,
        _100K,
        _125K,
        _200K,
        _250K,
        _400K,
        _500K,
        _800K,
        _1M,
    }
}
