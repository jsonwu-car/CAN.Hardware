namespace CAN.Hardware.Comm
{
    /// <summary>
    /// LIN 校验方式，对应 ZCAN_LIN_INIT_CONFIG.chkSumMode(eZLINChkSumMode)。
    /// 发布表/订阅表的 chkSumMode 另有一套取值：0=默认(启动时配置)、1=经典、2=增强。
    /// </summary>
    public enum ZlgLinChkSumMode : byte
    {
        Classic = 1,
        Enhanced = 2,
        Auto = 3,
    }
}
