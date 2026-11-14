using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 广成科技 USBCANFD 标志位解析器,产出 <see cref="CanFrameFlags"/>。
    /// CANFD_OBJ.CanORCanfdType 是一个按位打包的字节:
    /// bit0 = CAN/CANFD,bit1 = STD/EXT,bit2 = DATA/RTR,bit3 = 数据波特率切换(BRS),
    /// bit4~7 保留。详见 <see cref="BaseLibDll.GuangChengCanFdLibDLL.BuildFrameType"/>。
    /// </summary>
    internal static class GuangChengCanFdFlagParser
    {
        internal const byte PROTO_CANFD = 0x01;
        internal const byte FORMAT_EXTENDED = 0x02;
        internal const byte TYPE_RTR = 0x04;
        internal const byte BITRATE_SWITCH = 0x08;

        /// <summary>
        /// </summary>
        /// <param name="rawType">CANFD_OBJ.CanORCanfdType 原始字节</param>
        public static CanFrameFlags Parse(byte rawType)
        {
            bool isFd = (rawType & PROTO_CANFD) != 0;
            bool isExtended = (rawType & FORMAT_EXTENDED) != 0;
            return new CanFrameFlags
            {
                RawFlags = rawType,

                IsRTR = (rawType & TYPE_RTR) != 0,
                IsStandardFrame = !isExtended,
                IsExtendedFrame = isExtended,

                IsCanFd = isFd,
                HasBrs = isFd && (rawType & BITRATE_SWITCH) != 0,
            };
        }
    }
}
