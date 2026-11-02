using CAN.Hardware.Interface;
using static CAN.Hardware.BaseLibDll.EbElFcbaseLibDLL;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// EbEl(FlexCard) CAN 标志位解析器,将 fcCANPacket 的 ID(bit29=扩展帧)
    /// 与 FlagsAndDLC 字节(bit4=方向, bit5=RTR, bit6=丢帧)解析为统一的 <see cref="CanFrameFlags"/>。
    /// </summary>
    internal static class EbElCanFlagParser
    {
        public static CanFrameFlags Parse(uint rawId, byte flagsAndDlc)
        {
            var f = (DLCFlags)flagsAndDlc;
            bool isTxAck = (f & DLCFlags.Direction) != 0;
            bool messageLost = (f & DLCFlags.MessageLost) != 0;

            return new CanFrameFlags
            {
                RawFlags = flagsAndDlc,

                IsRTR = (f & DLCFlags.RemoteFrame) != 0,
                IsStandardFrame = (rawId & FC_EXT_FLAG) == 0,
                IsExtendedFrame = (rawId & FC_EXT_FLAG) != 0,

                IsTxAck = isTxAck,

                HasOverrun = messageLost,
                HasHwOverrun = messageLost,
            };
        }
    }
}
