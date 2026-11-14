using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 广成科技经典 CAN 标志位解析器,产出 <see cref="CanFrameFlags"/>。
    /// ECanVci 的 CAN_OBJ.ID 是裸 ID,扩展帧/远程帧分别由 ExternFlag/RemoteFlag 表示;
    /// 厂商接口没有错误帧字段,因此错误标志恒为 false。
    /// </summary>
    internal static class GuangChengCanFlagParser
    {
        /// <summary>
        /// </summary>
        /// <param name="rawId">裸 CAN ID(不含标志位)</param>
        /// <param name="remoteFlag">RemoteFlag:0=数据帧,1=远程帧</param>
        /// <param name="externFlag">ExternFlag:0=标准帧,1=扩展帧</param>
        public static CanFrameFlags Parse(uint rawId, byte remoteFlag, byte externFlag)
        {
            bool isExtended = externFlag != 0;
            return new CanFrameFlags
            {
                RawFlags = rawId,

                IsRTR = remoteFlag != 0,
                IsStandardFrame = !isExtended,
                IsExtendedFrame = isExtended,
            };
        }
    }
}
