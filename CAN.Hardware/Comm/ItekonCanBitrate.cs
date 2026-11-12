namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技经典 CAN 波特率。
    /// ControlCAN(VCI) 使用 Timing0/Timing1 寄存器值,枚举与寄存器的对应关系见
    /// <see cref="BaseLibDll.ItekonCanLibDLL.BuildTiming"/>。
    /// </summary>
    public enum ItekonCanBitrate
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
        _666K,
        _800K,
        _1M,
    }
}
