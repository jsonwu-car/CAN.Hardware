using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 同星(ToSun) CAN/CANFD 标志位解析器,产出 <see cref="CanFrameFlags"/>。
    /// FProperties 位定义:bit0 Tx,bit1 Remote,bit2 Ext,bit6 Logged,bit7 Error;
    /// FFDProperties 位定义:bit0 FD,bit1 BRS,bit2 ESI。
    /// </summary>
    internal static class ToSunCanFlagParser
    {
        private const uint PROP_TX = 0x01;
        private const uint PROP_REMOTE = 0x02;
        private const uint PROP_EXT = 0x04;
        private const uint PROP_ERROR = 0x80;

        private const uint FD_PROP_FDF = 0x01;
        private const uint FD_PROP_BRS = 0x02;
        private const uint FD_PROP_ESI = 0x04;

        /// <summary>经典 CAN(TLIBCAN.FProperties)</summary>
        public static CanFrameFlags Parse(uint fProperties)
        {
            return Parse(fProperties, 0);
        }

        /// <summary>CAN FD(TLIBCANFD.FProperties + FFDProperties)</summary>
        public static CanFrameFlags Parse(uint fProperties, uint fdProperties)
        {
            bool isExt = (fProperties & PROP_EXT) != 0;
            return new CanFrameFlags
            {
                RawFlags = fProperties | (fdProperties << 8),

                IsRTR = (fProperties & PROP_REMOTE) != 0,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,

                IsTxAck = (fProperties & PROP_TX) != 0,

                IsCanFd = (fdProperties & FD_PROP_FDF) != 0,
                HasBrs = (fdProperties & FD_PROP_BRS) != 0,
                HasEsi = (fdProperties & FD_PROP_ESI) != 0,

                IsErrorFrame = (fProperties & PROP_ERROR) != 0,
            };
        }
    }
}
