using System;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// PEAK LIN(PLIN API)标志位解析器。
    /// TLINRcvMsg.Direction 为 dirPublisher 表示本机发布的帧(发送回显),
    /// 其余为总线接收帧;ErrorFlags 为厂商原始错误标志位。
    /// </summary>
    internal static class PeakLinFlagParser
    {
        public static LinFrameFlags Parse(TLINRcvMsg msg)
        {
            bool isTx = msg.Direction == TLINDirection.dirPublisher;
            int errorFlags = (int)msg.ErrorFlags;
            return new LinFrameFlags
            {
                RawFlags = (uint)errorFlags,
                IsTx = isTx,
                IsRx = !isTx,
                ErrorStatus = errorFlags == 0 ? (byte)0 : (byte)Math.Min(errorFlags, byte.MaxValue),
                MessageInfo = new LinMessageInfoData
                {
                    CheckSum = msg.Checksum,
                    IdPar = msg.FrameId,
                    SynchEdgeTimes = new uint[4],
                    ByteTimes = new uint[8],
                }
            };
        }
    }
}
