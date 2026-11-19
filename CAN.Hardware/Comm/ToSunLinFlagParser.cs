using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 同星 LIN(libTSCAN TLIBLIN)标志位解析器。
    /// FProperties: bit0=本机发送, bit1=发送Break, bit2=收到Break, bit3=收到同步场,
    /// bit7=错误帧;FErrStatus 为厂商原始错误状态(0 表示无错误)。
    /// </summary>
    internal static class ToSunLinFlagParser
    {
        private const byte MaskIsSendBreak = 0x02;
        private const byte MaskIsReceivedBreak = 0x04;
        private const byte MaskIsReceivedSync = 0x08;
        private const byte MaskIsErrorFrame = 0x80;

        public static LinFrameFlags Parse(TLIBLIN msg)
        {
            LinFrameFlags flags = new LinFrameFlags
            {
                RawFlags = msg.FProperties,
                IsTx = msg.FIsTx,
                IsRx = !msg.FIsTx,
                IsSendBreak = (msg.FProperties & MaskIsSendBreak) != 0,
                IsReceivedBreak = (msg.FProperties & MaskIsReceivedBreak) != 0,
                IsReceivedSync = (msg.FProperties & MaskIsReceivedSync) != 0,
                ErrorStatus = msg.FErrStatus != 0 ? msg.FErrStatus : (byte)((msg.FProperties & MaskIsErrorFrame) != 0 ? MaskIsErrorFrame : 0),
                MessageInfo = new LinMessageInfoData
                {
                    CheckSum = msg.FChecksum,
                    IdPar = CalculatePid(msg.FIdentifier),
                    SynchEdgeTimes = new uint[4],
                    ByteTimes = new uint[8],
                }
            };
            return flags;
        }

        /// <summary>
        /// 由 6 位 LIN ID 计算带奇偶校验位的 PID 字节。
        /// </summary>
        public static byte CalculatePid(byte id)
        {
            id &= 0x3F;
            int p0 = ((id >> 0) ^ (id >> 1) ^ (id >> 2) ^ (id >> 4)) & 0x01;
            int p1 = (~((id >> 1) ^ (id >> 3) ^ (id >> 4) ^ (id >> 5))) & 0x01;
            return (byte)(id | (p0 << 6) | (p1 << 7));
        }
    }
}
