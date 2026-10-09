namespace CAN.Hardware.Interface
{
    /// <summary>
    /// 不同硬件、总线协议数据帧的抽象
    /// </summary>
    public abstract class FrameBase
    {
        public long ID { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 解析后的标志位信息,主要应用在Rx报文中，不同总线类型对应不同的子类,
        /// (CAN/CANFD 为 <see cref="CanFrameFlags"/>),未解析时为 null。
        /// </summary>
        public virtual BusFrameFlags? Flags { get; internal set; }
    }
}
