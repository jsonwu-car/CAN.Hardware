using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 图莫斯经典 CAN(CAN_MSG)标志位解析器。
    /// CAN_MSG.ID 为裸 ID;RemoteFlag bit0=RTR,bit6..5=通道号,bit7=发送帧标志;
    /// ExternFlag 单独表示扩展帧。
    /// </summary>
    internal static class ToomossCanFlagParser
    {
        public const byte RemoteFlagRtr = 0x01;
        public const byte RemoteFlagTx = 0x80;
        public const byte ExternFlagExt = 0x01;
        public const byte ExternFlagErr = 0x80;

        public static CanFrameFlags Parse(uint id, byte remoteFlag, byte externFlag, bool isTx)
        {
            bool isExt = (externFlag & ExternFlagExt) != 0;
            bool isErr = (externFlag & ExternFlagErr) != 0;
            bool isRtr = (remoteFlag & RemoteFlagRtr) != 0;

            uint rawId = id & CanFrameDef.CAN_ID_FLAG;
            if (isExt) rawId |= CanFrameDef.CAN_EFF_FLAG;
            if (isRtr) rawId |= CanFrameDef.CAN_RTR_FLAG;
            if (isErr) rawId |= CanFrameDef.CAN_ERR_FLAG;

            return new CanFrameFlags
            {
                //设备原始标志分散在两个字节中,这里打包保留
                RawFlags = (uint)((externFlag << 8) | remoteFlag),

                IsRTR = isRtr,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,
                IsErrorFrame = isErr,
                IsTxRequest = isTx,

                //经典 CAN 通道无 FD 属性
                IsCanFd = false,
                HasBrs = false,
                HasEsi = false,
            };
        }
    }
}
