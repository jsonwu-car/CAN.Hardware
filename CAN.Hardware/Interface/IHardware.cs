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
        /// 清除接口缓冲区的报文
        /// </summary>
        void FlushReceiveBuffer();

        /// <summary>
        /// 清除发送缓冲区的报文
        /// </summary>
        void FlushTransmitBuffer();
    }
}
