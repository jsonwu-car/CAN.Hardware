using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// ZLG USBCAN 标志位解析器,产出 <see cref="CanFrameFlags"/>。
    /// 帧类型信息编码在 can_id 高位(SocketCAN 风格,见 <see cref="CanFrameDef"/>);
    /// CAN FD 的 BRS/ESI 在 canfd_frame.flags:bit0 BRS,bit1 ESI。
    /// </summary>
    internal static class ZlgCanFlagParser
    {
        private const byte CANFD_BRS = 0x01;
        private const byte CANFD_ESI = 0x02;

        /// <summary>
        /// </summary>
        /// <param name="rawCanId">原始 can_id(未剥离高位标志)</param>
        /// <param name="fdFlags">CAN FD 帧的 flags 字节,经典 CAN 传 0</param>
        /// <param name="isFd">是否 CAN FD 通道</param>
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
