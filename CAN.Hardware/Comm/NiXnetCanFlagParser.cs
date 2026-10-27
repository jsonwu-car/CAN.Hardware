using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// NI-XNET CAN/CANFD 标志位解析器,
    /// 输入为 nxFrame_t 帧头中的 Identifier/Type/Flags 原始值,产出 <see cref="CanFrameFlags"/>。
    /// </summary>
    internal static class NiXnetCanFlagParser
    {
        public static CanFrameFlags Parse(uint identifier, byte frameType, byte frameFlags)
        {
            bool isExt = (identifier & NiXnetLibDLL.NX_CAN_EXT_FLAG) != 0;
            bool isEcho = (frameFlags & NiXnetLibDLL.NX_FRAME_FLAG_TRANSMIT_ECHO) != 0;
            return new CanFrameFlags
            {
                // 原始值合成: 高 3 位为 Identifier 标志位(bit29=扩展帧), 次高字节为 Flags, 低字节为 Type
                RawFlags = (identifier & 0xE0000000) | (uint)(frameFlags << 8) | frameType,
                IsRTR = frameType == NiXnetLibDLL.NX_FRAME_TYPE_CAN_REMOTE,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,
                IsCanFd = (frameType & NiXnetLibDLL.NX_FRAME_TYPE_CANFD_DATA) != 0,
                HasBrs = (frameType & NiXnetLibDLL.NX_FRAME_TYPE_CANFDBRS_DATA) == NiXnetLibDLL.NX_FRAME_TYPE_CANFDBRS_DATA,
                IsErrorFrame = frameType == NiXnetLibDLL.NX_FRAME_TYPE_CAN_BUS_ERROR,
                IsTxAck = isEcho,
                IsLocalEcho = isEcho,
            };
        }
    }
}
