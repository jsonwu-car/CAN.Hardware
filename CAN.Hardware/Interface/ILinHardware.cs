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

        /// <summary>
        /// 对指定 LIN 帧 ID 设置错误注入：可选择反转校验和、反转 ID 奇偶位，
        /// 并可在帧头之后、首字节之前插入延迟（单位：位时间）。
        /// 两个开关都为 false 且 delayBitTimes 为 0 时恢复正常发送。
        /// 帧 ID 非法、通道未打开或设置失败时抛出异常；返回 true 表示驱动已接受设置。
        /// 注意：一次调用会覆盖该帧 ID 之前的干扰配置，不能同时保留两种独立状态。
        /// </summary>
        bool SetIllegalMessage(long linId, bool checksumError, bool parityError, uint delayBitTimes);

        /// <summary>
        /// 清除从节点指定 LIN 帧 ID 的响应：下一次主节点轮询该帧 ID 时从节点不再应答。
        /// 仅从节点模式有效，通常要求通道已上线（on-bus）。
        /// 恢复响应：从节点模式下对该帧 ID 重新调用 Send 武装响应数据即可。
        /// 帧 ID 非法、通道未打开、当前不是从节点或清除失败时抛出异常；返回 true 表示驱动已接受清除。
        /// </summary>
        bool ClearMessage(long linId);
    }
}
