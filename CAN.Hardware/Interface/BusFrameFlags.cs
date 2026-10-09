namespace CAN.Hardware.Interface
{
    /// <summary>
    /// 总线帧解析后的标志位信息基类。
    /// 不同总线(CAN/LIN等)解析结果不同,由各自的子类表达,
    /// 通过 <see cref="FrameBase.Flags"/> 对外暴露。
    /// </summary>
    public abstract class BusFrameFlags
    {
        /// <summary>
        /// 设备返回的原始 flag 值
        /// </summary>
        public uint RawFlags { get; internal set; }

        /// <summary>
        /// 该帧是否为错误帧/包含错误
        /// </summary>
        public abstract bool IsError { get; }

        /// <summary>
        /// 人类可读的标志位摘要
        /// </summary>
        public abstract override string ToString();
    }
}
