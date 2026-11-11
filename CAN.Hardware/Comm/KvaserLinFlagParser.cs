using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// Kvaser LIN(linlib)标志位解析器。
    /// linReadMessage 的 flags 是 LIN_xxx 位的组合:bit0 TX(本机发送),bit1 RX(总线接收),
    /// 其余位(唤醒/无数据/错误组)保留在 RawFlags 中,不单独映射。
    /// linlib 无独立的 Break/Sync 标志位,IsSendBreak/IsReceivedBreak/IsReceivedSync 恒为 false。
    /// </summary>
    internal static class KvaserLinFlagParser
    {
        private const uint LIN_MSGFLAG_TX = 0x01;
        private const uint LIN_MSGFLAG_RX = 0x02;

        public static LinFrameFlags Parse(uint flags)
        {
            return new LinFrameFlags
            {
                RawFlags = flags,
                IsTx = (flags & LIN_MSGFLAG_TX) != 0,
                IsRx = (flags & LIN_MSGFLAG_RX) != 0,
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
