using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// ESD CAN/CANFD 标志位解析器,产出 <see cref="CanFrameFlags"/>。
    /// ESD 的帧属性集中在 BaseMsg.Len 字节(bit0-3=DLC, bit4=RTR/NO_BRS, bit7=FD)
    /// 与原始 ID 的 bit29(扩展帧);ESI 在 BaseMsg.Esi,丢帧计数在 BaseMsg.MsgLost。
    /// </summary>
    internal static class EsdCanFlagParser
    {
        /// <summary>解析 ntcan 消息头为统一标志位。rawId 为驱动原始 ID(含 bit29 扩展标记)。</summary>
        public static CanFrameFlags Parse(uint rawId, byte len, byte esi, byte msgLost)
        {
            bool isExt = (rawId & EsdNtcanLibDLL.NTCAN_EXT_FLAG) != 0;
            bool isFd = (len & EsdNtcanLibDLL.LEN_FLAG_FD) != 0;
            return new CanFrameFlags
            {
                RawFlags = len,

                // FD 帧内 bit4 复用为 NO_BRS(置位=不加速),经典帧才是 RTR
                IsRTR = !isFd && (len & EsdNtcanLibDLL.LEN_FLAG_RTR) != 0,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,

                IsCanFd = isFd,
                HasBrs = isFd && (len & EsdNtcanLibDLL.LEN_FLAG_FD_NO_BRS) == 0,
                HasEsi = esi != 0,

                // 驱动上报的丢帧计数映射为溢出
                HasOverrun = msgLost != 0,
            };
        }
    }
}
