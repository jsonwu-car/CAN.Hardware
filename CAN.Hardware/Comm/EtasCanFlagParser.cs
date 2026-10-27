using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// ETAS BOA CAN/CANFD 标志位解析器,
    /// 输入为 OCI 消息的 MsgFlags(u16) 原始值,产出 <see cref="CanFrameFlags"/>。
    /// </summary>
    internal static class EtasCanFlagParser
    {
        public static CanFrameFlags Parse(ushort msgFlags)
        {
            bool isExt = (msgFlags & EtasBoaLibDLL.MSG_FLAG_EXTENDED) != 0;
            return new CanFrameFlags
            {
                RawFlags = msgFlags,
                IsRTR = (msgFlags & EtasBoaLibDLL.MSG_FLAG_REMOTE_FRAME) != 0,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,
                IsCanFd = (msgFlags & EtasBoaLibDLL.MSG_FLAG_FD_DATA) != 0,
                HasBrs = (msgFlags & EtasBoaLibDLL.MSG_FLAG_FD_DATA_BIT_RATE) != 0,
                HasEsi = (msgFlags & EtasBoaLibDLL.MSG_FLAG_FD_ERROR_PASSIVE) != 0,
                IsLocalEcho = (msgFlags & EtasBoaLibDLL.MSG_FLAG_SELFRECEPTION) != 0,
            };
        }
    }
}
