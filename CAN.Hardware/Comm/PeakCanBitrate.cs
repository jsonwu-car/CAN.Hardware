
namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Peak CAN 波特率,枚举值即 PCAN-Basic 的 BTR0BTR1 编码
    /// </summary>
    public enum PeakCanBitrate : ushort
    {
        pcanBAUD_50K = 0x472F,
        pcanBAUD_100K = 0x432F,
        pcanBAUD_125K = 0x031C,
        pcanBAUD_250K = 0x011C,
        pcanBAUD_500K = 0x001C,
        pcanBAUD_1M = 0x0014,
    }
}
