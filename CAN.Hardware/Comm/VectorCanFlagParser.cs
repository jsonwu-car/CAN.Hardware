using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Vector CAN/CANFD 标志位解析器,产出 <see cref="CanFrameFlags"/>。
    /// 经典 CAN 使用 XLevent.msgFlags(<see cref="XLcanMsgFlags"/>);
    /// CANFD 使用 XLcanRxEvent.msgFlags(<see cref="XLcanFdRxFlags"/>);
    /// 扩展帧标志不在 flags 中,位于原始 ID 的 bit31(XL_CAN_EXT_MSG_ID)。
    /// </summary>
    internal static class VectorCanFlagParser
    {
        /// <summary>经典 CAN(XLevent.msgFlags + 原始ID)</summary>
        public static CanFrameFlags Parse(uint msgFlags, uint rawId)
        {
            var f = (XLcanMsgFlags)msgFlags;
            bool isExt = (rawId & VectorCanLibDLL.XL_CAN_EXT_MSG_ID) != 0;
            return new CanFrameFlags
            {
                RawFlags = msgFlags,

                IsRTR = (f & XLcanMsgFlags.REMOTE_FRAME) != 0,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,
                IsWakeup = (f & XLcanMsgFlags.WAKEUP) != 0,
                IsNerr = (f & XLcanMsgFlags.NERR) != 0,

                IsTxAck = (f & XLcanMsgFlags.TX_COMPLETED) != 0,
                IsTxRequest = (f & XLcanMsgFlags.TX_REQUEST) != 0,

                IsErrorFrame = (f & XLcanMsgFlags.ERROR_FRAME) != 0,
                HasOverrun = (f & XLcanMsgFlags.OVERRUN) != 0,
            };
        }

        /// <summary>CAN FD(XLcanRxEvent.msgFlags + 原始ID),isTxAck 表示发送确认事件。</summary>
        public static CanFrameFlags ParseFd(uint msgFlags, uint rawId, bool isTxAck = false)
        {
            var f = (XLcanFdRxFlags)msgFlags;
            bool isExt = (rawId & VectorCanLibDLL.XL_CAN_EXT_MSG_ID) != 0;
            bool isError = (f & XLcanFdRxFlags.EF) != 0;
            bool isTe = (f & XLcanFdRxFlags.TE) != 0;
            return new CanFrameFlags
            {
                RawFlags = msgFlags,

                IsRTR = (f & XLcanFdRxFlags.RTR) != 0,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,
                IsWakeup = (f & XLcanFdRxFlags.WAKEUP) != 0,

                IsCanFd = (f & XLcanFdRxFlags.EDL) != 0,
                HasBrs = (f & XLcanFdRxFlags.BRS) != 0,
                HasEsi = (f & XLcanFdRxFlags.ESI) != 0,

                IsTxAck = isTxAck,
                IsArbitrationLost = (f & XLcanFdRxFlags.ARB_LOST) != 0,

                IsErrorFrame = isError,
                HasBusError = isError || isTe,
            };
        }
    }
}
