using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Peak(PCAN-Basic) CAN/CANFD 标志位解析器,
    /// 输入为 TPCANMessageType(MSGTYPE)的原始值,产出 <see cref="CanFrameFlags"/>。
    /// </summary>
    internal static class PeakCanFlagParser
    {
        public static CanFrameFlags Parse(uint msgType)
        {
            var f = (TPCANMessageType)msgType;
            bool isExt = f.Has(TPCANMessageType.PCAN_MESSAGE_EXTENDED);
            return new CanFrameFlags
            {
                RawFlags = msgType,

                IsRTR = f.Has(TPCANMessageType.PCAN_MESSAGE_RTR),
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,

                IsCanFd = f.Has(TPCANMessageType.PCAN_MESSAGE_FD),
                HasBrs = f.Has(TPCANMessageType.PCAN_MESSAGE_BRS),
                HasEsi = f.Has(TPCANMessageType.PCAN_MESSAGE_ESI),

                IsErrorFrame = f.Has(TPCANMessageType.PCAN_MESSAGE_ERRFRAME),
            };
        }

        private static bool Has(this TPCANMessageType value, TPCANMessageType flag)
        {
            return (value & flag) == flag;
        }
    }
}
