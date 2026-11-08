using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 创芯科技 CANFD(ControlCANFD ZCAN)标志位解析器。
    /// can_frame/canfd_frame 的 can_id 高位为 SocketCAN 风格标志(bit31 EFF/bit30 RTR/bit29 ERR);
    /// CAN FD 的 BRS/ESI 在 canfd_frame.flags:bit0 BRS,bit1 ESI。
    /// </summary>
    internal static class ChuangXinCanFdFlagParser
    {
        private const byte CANFD_BRS = 0x01;
        private const byte CANFD_ESI = 0x02;

        public static CanFrameFlags Parse(uint rawCanId, byte fdFlags, bool isFd)
        {
            bool isExt = (rawCanId & CanFrameDef.CAN_EFF_FLAG) != 0;
            return new CanFrameFlags
            {
                RawFlags = rawCanId,

                IsRTR = (rawCanId & CanFrameDef.CAN_RTR_FLAG) != 0,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,
                IsErrorFrame = (rawCanId & CanFrameDef.CAN_ERR_FLAG) != 0,

                IsCanFd = isFd,
                HasBrs = isFd && (fdFlags & CANFD_BRS) != 0,
                HasEsi = isFd && (fdFlags & CANFD_ESI) != 0,
            };
        }
    }
}
