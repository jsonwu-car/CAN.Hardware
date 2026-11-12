using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技经典 CAN(ControlCAN VCI)标志位解析器。
    /// VCI_CAN_OBJ 的 ID 不含标志位,扩展帧/远程帧分别由 ExternFlag/RemoteFlag 表示;
    /// 这里统一合成为工程约定的 SocketCAN 风格 ID 并产出 <see cref="CanFrameFlags"/>。
    /// </summary>
    internal static class ItekonCanFlagParser
    {
        public static CanFrameFlags Parse(uint id, byte remoteFlag, byte externFlag)
        {
            bool isExt = externFlag != 0;
            bool isRtr = remoteFlag != 0;

            uint rawId = id & CanFrameDef.CAN_ID_FLAG;
            if (isExt) rawId |= CanFrameDef.CAN_EFF_FLAG;
            if (isRtr) rawId |= CanFrameDef.CAN_RTR_FLAG;

            return new CanFrameFlags
            {
                RawFlags = rawId,

                IsRTR = isRtr,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,

                //经典 CAN 通道无 FD 属性
                IsCanFd = false,
                HasBrs = false,
                HasEsi = false,
            };
        }
    }
}
