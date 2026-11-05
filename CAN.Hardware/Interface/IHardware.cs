using CAN.Hardware.Model;

namespace CAN.Hardware.Interface
{
    /// <summary>
    /// 统一对外暴露的硬件接口
    /// </summary>
    public interface IHardware
    {
        /// <summary>
        /// 用于对外推送报文
        /// </summary>
        EventHandler<FrameBase>? eventHandler { get; set; }

        /// <summary>
        /// 用于硬件的配置(用于硬件的初始化，亦可用于WPF Xaml的Binding)
        /// </summary>
        BusConfigBase Config { get; }

        /// <summary>
        /// 硬件的Open
        /// </summary>
        bool Open();

        /// <summary>
        /// 硬件的Close
        /// </summary>
        bool Close();

        /// <summary>
        /// 报文的发送
        /// </summary>
        bool Send(FrameBase frame);

        /// <summary>
        /// 主动读取一帧报文(请求-响应式总线使用,如 LIN 主节点向从机请求应答)。
        /// 传入 frame 携带请求信息(如 LIN 帧的 ID),从机应答帧写回 frame。
        /// 默认实现返回 false:纯事件推送型硬件(CAN 系列)无需实现,报文统一由 <see cref="eventHandler"/> 推送。
        /// </summary>
        bool Read(ref FrameBase? frame) => false;

        /// <summary>
        /// 清除接口缓冲区的报文
        /// </summary>
        void FlushReceiveBuffer();

        /// <summary>
        /// 清除发送缓冲区的报文
        /// </summary>
        void FlushTransmitBuffer();
    }
}
