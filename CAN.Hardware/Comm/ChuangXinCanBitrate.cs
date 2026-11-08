namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 创芯科技经典 CAN 波特率。
    /// ControlCAN(VCI) 使用 Timing0/Timing1 寄存器值,枚举与寄存器的对应关系见
    /// <see cref="BaseLibDll.ChuangXinCanLibDLL.BuildTiming"/>。
    /// </summary>
    public enum ChuangXinCanBitrate
    {
        _50K,
        _100K,
        _125K,
        _250K,
        _500K,
        _800K,
        _1M,
    }
}
