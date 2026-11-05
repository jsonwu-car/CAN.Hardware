namespace CAN.Hardware.Interface
{
    /// <summary>
    /// LIN 总线数据帧。
    /// ID 为 6 位有效(0x00~0x3F),无扩展帧/远程帧概念;
    /// 数据长度 DLC 为 1~8(可变 DLC 时由帧 ID 决定)。
    /// </summary>
    public class LINFrame : FrameBase
    {
        /// <summary>数据长度(1~8)</summary>
        public int DLC { get; set; }

        /// <summary>驱动返回的原始标志位</summary>
        public int Flag { get; set; }

        /// <summary>时间戳(驱动原始单位)</summary>
        public ulong Timestamp { get; set; }

        /// <summary>方向:0=发送,1=接收</summary>
        public int Dir { get; set; }
    }
}
