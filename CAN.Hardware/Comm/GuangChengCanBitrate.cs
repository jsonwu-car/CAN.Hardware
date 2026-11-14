namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 广成科技经典 CAN 波特率。
    /// ECanVci 使用 SJA1000 风格的 Timing0(BTR0)/Timing1(BTR1) 寄存器值,
    /// 枚举与寄存器值的对应关系见 <see cref="BaseLibDll.GuangChengCanLibDLL.BuildTiming"/>。
    /// </summary>
    public enum GuangChengCanBitrate
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
