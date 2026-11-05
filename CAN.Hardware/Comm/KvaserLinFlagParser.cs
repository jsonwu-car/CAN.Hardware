using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Kvaser LIN(linlib)标志位解析器。
    /// linReadMessage 的 flags 是 LIN_xxx 位的组合:bit0 TX(本机发送),bit1 RX(总线接收),
    /// bit2 唤醒帧,bit3 无数据,bit4 校验和错误,bit5 奇偶校验错误,bit6 同步错误,bit7 位错误。
    /// linlib 无独立的 Break/Sync 标志位,IsSendBreak/IsReceivedBreak/IsReceivedSync 恒为 false。
    /// </summary>
    internal static class KvaserLinFlagParser
    {
        private const uint LIN_MSGFLAG_TX = 0x01;
        private const uint LIN_MSGFLAG_RX = 0x02;
        private const uint LIN_MSGFLAG_ERROR_BITS = LIN_MSGFLAG_CSUM_ERROR
            | LIN_MSGFLAG_PARITY_ERROR | LIN_MSGFLAG_SYNCH_ERROR | LIN_MSGFLAG_BIT_ERROR;

        // 与 Linlib 的 LIN_xxx 常量保持一致(重复定义一份,避免 P/Invoke 类泄漏到解析器)
        private const uint LIN_MSGFLAG_CSUM_ERROR = 0x10;
        private const uint LIN_MSGFLAG_PARITY_ERROR = 0x20;
        private const uint LIN_MSGFLAG_SYNCH_ERROR = 0x40;
        private const uint LIN_MSGFLAG_BIT_ERROR = 0x80;

        public static LinFrameFlags Parse(uint flags)
        {
            // 错误位原样映射到 ErrorStatus(bit4~bit7),IsError 即非零
            byte errorStatus = (byte)(flags & LIN_MSGFLAG_ERROR_BITS);
            return new LinFrameFlags
            {
                RawFlags = flags,
                IsTx = (flags & LIN_MSGFLAG_TX) != 0,
                IsRx = (flags & LIN_MSGFLAG_RX) != 0,
                ErrorStatus = errorStatus,
            };
        }

        /// <summary>
        /// 接收路径使用:同时解析 linReadMessage 返回的 <see cref="KvaserLinLibDLL.LinMessageInfo"/>。
        /// </summary>
        public static unsafe LinFrameFlags Parse(uint flags, KvaserLinLibDLL.LinMessageInfo msgInfo)
        {
            LinFrameFlags result = Parse(flags);
            var info = new LinMessageInfoData
            {
                SynchBreakLength = msgInfo.synchBreakLength,
                FrameLength = msgInfo.frameLength,
                Bitrate = msgInfo.bitrate,
                CheckSum = msgInfo.checkSum,
                IdPar = msgInfo.idPar,
                SynchEdgeTimes = new uint[4],
                ByteTimes = new uint[8],
            };
            for (int i = 0; i < 4; i++) info.SynchEdgeTimes[i] = msgInfo.synchEdgeTime[i];
            for (int i = 0; i < 8; i++) info.ByteTimes[i] = msgInfo.byteTime[i];
            result.MessageInfo = info;
            return result;
        }
    }
}
