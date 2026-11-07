namespace CAN.Hardware.Interface
{
    /// <summary>
    /// LIN 硬件接口：在统一 <see cref="IHardware"/> 之上增加 LIN 专用能力。
    /// </summary>
    public interface ILinHardware : IHardware
    {
        /// <summary>
        /// 主节点发送帧头请求从节点响应;响应帧由后台接收任务经 eventHandler 异步上抛(仅 Master 模式有效)
        /// </summary>
        bool RequestMessage(long linId);

        /// <summary>
        /// 向总线发送唤醒信号(ISO 17987:250µs~5ms 显性电平),唤醒休眠的从节点(仅 Master 模式有效)。
        /// 唤醒后从节点需要就绪时间(规范要求唤醒信号结束后 100ms 内可接收),调用方需自行延时后再发请求。
        /// 返回 false 表示未连接/非主节点/本次唤醒未发出。
        /// </summary>
        bool WakeUp();

        /// <summary>
        /// 运行时设置LIN通道校验和类型(false=经典校验,true=增强校验),对后续发送的帧生效。
        /// 未连接时仅更新配置(下次 Open 生效);返回 false 表示硬件不支持运行时切换或设置失败。
        /// </summary>
        bool SetChecksumType(bool enhanced);
    }
}
