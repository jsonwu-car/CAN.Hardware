using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 图莫斯 CANFD(CANFD_MSG)标志位解析器。
    /// CANFD_MSG.ID: bit31=IDE, bit30=RTR, bit28..0=ID;
    /// CANFD_MSG.Flags: bit0=BRS, bit1=ESI, bit2=FDF, bit6..5=通道号, bit7=发送帧标志。
    /// </summary>
    internal static class ToomossCanFdFlagParser
    {
        public const uint IdFlagIde = 0x80000000;
        public const uint IdFlagRtr = 0x40000000;
        public const uint IdFlagMask = 0x1FFFFFFF;

        public const byte FlagBrs = 0x01;
        public const byte FlagEsi = 0x02;
        public const byte FlagFdf = 0x04;
        public const byte FlagTx = 0x80;

        public static CanFrameFlags Parse(uint id, byte flags, bool isTx)
        {
            bool isExt = (id & IdFlagIde) != 0;
            return new CanFrameFlags
            {
                RawFlags = flags,

                IsRTR = (id & IdFlagRtr) != 0,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,
                IsTxRequest = isTx,

                IsCanFd = (flags & FlagFdf) != 0,
                HasBrs = (flags & FlagBrs) != 0,
                HasEsi = (flags & FlagEsi) != 0,
            };
        }
    }
}
