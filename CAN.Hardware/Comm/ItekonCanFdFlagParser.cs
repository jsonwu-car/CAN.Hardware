using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 爱泰科技 CANFD(iTekCANFD)标志位解析器。
    /// iTek 的 can_id 高 3 位:bit31 保留,bit30 扩展帧,bit29 远程帧;
    /// cantype:0=CAN,2=CANFD,3=CANFD 加速(BRS)。
    /// 这里统一转换为工程约定的 SocketCAN 风格 ID(bit31 EFF/bit30 RTR)并产出
    /// <see cref="CanFrameFlags"/>。
    /// </summary>
    internal static class ItekonCanFdFlagParser
    {
        private const uint ITEK_CAN_EFF_FLAG = 0x40000000U;
        private const uint ITEK_CAN_RTR_FLAG = 0x20000000U;

        /// <summary>接收帧 cantype:CAN</summary>
        public const byte CAN_TYPE_CAN = 0;
        /// <summary>接收帧 cantype:CANFD</summary>
        public const byte CAN_TYPE_CANFD = 2;
        /// <summary>接收帧 cantype:CANFD 加速(BRS)</summary>
        public const byte CAN_TYPE_CANFD_BRS = 3;
        /// <summary>cantype 低 2 位为帧类型,bit6(0x40)为发送方向标志</summary>
        public const byte CAN_TYPE_MASK = 0x03;

        public static CanFrameFlags Parse(uint hardwareCanId, byte hardwareFlags, byte canType)
        {
            bool isExt = (hardwareCanId & ITEK_CAN_EFF_FLAG) != 0;
            bool isRtr = (hardwareCanId & ITEK_CAN_RTR_FLAG) != 0;
            //接收帧可能带 0x40 方向位,比较前先屏蔽
            canType = (byte)(canType & CAN_TYPE_MASK);
            bool isFd = canType == CAN_TYPE_CANFD || canType == CAN_TYPE_CANFD_BRS;
            bool isBrs = canType == CAN_TYPE_CANFD_BRS;

            uint rawId = hardwareCanId & CanFrameDef.CAN_ID_FLAG;
            if (isExt) rawId |= CanFrameDef.CAN_EFF_FLAG;
            if (isRtr) rawId |= CanFrameDef.CAN_RTR_FLAG;

            return new CanFrameFlags
            {
                RawFlags = rawId,

                IsRTR = isRtr,
                IsStandardFrame = !isExt,
                IsExtendedFrame = isExt,

                IsCanFd = isFd,
                HasBrs = isBrs,
                HasEsi = false,

                //flags=1 表示错误帧,错误信息由 data0-data7 表达
                IsErrorFrame = hardwareFlags != 0,
            };
        }
    }
}
