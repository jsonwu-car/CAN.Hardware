using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// ZLG LIN(zlgcan ZCAN_LIN_MSG)标志位解析器。
    /// dir:0=接收、1=发送(本机回显)；错误帧的 errorStatus 为 errData 低字节
    /// (低 4 位=错误阶段，高 4 位=错误原因)，0 表示无错误。
    /// ZLG LIN 报文没有独立的 Break/Sync 标志位，IsSendBreak/IsReceivedBreak/
    /// IsReceivedSync 恒为 false；方向与错误状态分别由 dir/errorStatus 表达。
    /// </summary>
    internal static class ZlgLinFlagParser
    {
        public static LinFrameFlags Parse(byte dataType, byte dir, byte rawPid, byte chkSum, byte errorStatus)
        {
            bool isTx = dir == 1;
            return new LinFrameFlags
            {
                RawFlags = errorStatus,
                IsTx = isTx,
                IsRx = !isTx,
                ErrorStatus = errorStatus,
                MessageInfo = new LinMessageInfoData
                {
                    CheckSum = chkSum,
                    IdPar = rawPid,
                    SynchEdgeTimes = new uint[4],
                    ByteTimes = new uint[8],
                }
            };
        }
    }
}
