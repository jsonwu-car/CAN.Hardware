using System.Collections.Generic;
using System.Text;

namespace CAN.Hardware.Interface
{
    /// <summary>
    /// CAN/CANFD 总线帧解析后的标志位信息。
    /// 同一总线不同厂商的解析结果一致,由厂商各自的解析器填充。
    /// </summary>
    public class CanFrameFlags : BusFrameFlags
    {
        // ---------------- 帧类型 ----------------
        /// <summary>远程帧请求</summary>
        public bool IsRTR { get; internal set; }
        /// <summary>标准帧(11-bit ID)</summary>
        public bool IsStandardFrame { get; internal set; }
        /// <summary>扩展帧(29-bit ID)</summary>
        public bool IsExtendedFrame { get; internal set; }
        /// <summary>唤醒消息</summary>
        public bool IsWakeup { get; internal set; }
        /// <summary>NERR 激活</summary>
        public bool IsNerr { get; internal set; }

        // ---------------- CAN FD 属性(经典 CAN 恒为 false) ----------------
        /// <summary>CAN FD 帧</summary>
        public bool IsCanFd { get; internal set; }
        /// <summary>数据段使用比特率切换</summary>
        public bool HasBrs { get; internal set; }
        /// <summary>发送节点处于错误被动模式</summary>
        public bool HasEsi { get; internal set; }

        // ---------------- 收发状态 ----------------
        /// <summary>发送确认(已真正发送到总线)</summary>
        public bool IsTxAck { get; internal set; }
        /// <summary>发送请求(已交付 CAN 控制器等待发送)</summary>
        public bool IsTxRequest { get; internal set; }
        /// <summary>本地回显(来自同一通道的另一个句柄)</summary>
        public bool IsLocalEcho { get; internal set; }
        /// <summary>单次发送</summary>
        public bool IsSingleShot { get; internal set; }
        /// <summary>单次发送失败</summary>
        public bool IsTxFailed { get; internal set; }
        /// <summary>仲裁丢失(仅单次发送失败时有意义)</summary>
        public bool IsArbitrationLost { get; internal set; }
        /// <summary>延迟消息(未发送到总线)</summary>
        public bool IsDelayMessage { get; internal set; }

        // ---------------- 错误 ----------------
        /// <summary>错误帧</summary>
        public bool IsErrorFrame { get; internal set; }
        /// <summary>硬件缓冲区溢出</summary>
        public bool HasHwOverrun { get; internal set; }
        /// <summary>软件缓冲区溢出</summary>
        public bool HasSwOverrun { get; internal set; }
        /// <summary>任意溢出(硬件或软件)</summary>
        public bool HasOverrun { get; internal set; }
        /// <summary>位填充错误</summary>
        public bool HasStuffError { get; internal set; }
        /// <summary>帧格式错误</summary>
        public bool HasFormError { get; internal set; }
        /// <summary>CRC 校验错误</summary>
        public bool HasCrcError { get; internal set; }
        /// <summary>位错误(发送与读回电平不一致)</summary>
        public bool HasBitError { get; internal set; }
        /// <summary>任意总线错误(填充/格式/CRC/位错误)</summary>
        public bool HasBusError { get; internal set; }

        public override bool IsError
        {
            get { return IsErrorFrame || HasOverrun || HasBusError; }
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (IsStandardFrame) parts.Add("STD");
            if (IsExtendedFrame) parts.Add("EXT");
            if (IsRTR) parts.Add("RTR");
            if (IsCanFd) parts.Add("FDF");
            if (HasBrs) parts.Add("BRS");
            if (HasEsi) parts.Add("ESI");
            if (IsErrorFrame) parts.Add("ERROR_FRAME");
            if (HasHwOverrun) parts.Add("HW_OVERRUN");
            if (HasSwOverrun) parts.Add("SW_OVERRUN");
            if (HasStuffError) parts.Add("STUFF");
            if (HasFormError) parts.Add("FORM");
            if (HasCrcError) parts.Add("CRC");
            if (HasBitError) parts.Add("BIT");
            if (IsTxAck) parts.Add("TXACK");
            if (IsTxRequest) parts.Add("TXRQ");
            if (IsLocalEcho) parts.Add("LOCAL_TXACK");
            if (IsTxFailed) parts.Add("TXNACK");
            if (IsArbitrationLost) parts.Add("ABL");
            if (IsWakeup) parts.Add("WAKEUP");
            if (IsNerr) parts.Add("NERR");
            if (IsDelayMessage) parts.Add("DELAY_MSG");

            var sb = new StringBuilder();
            sb.Append("0x").Append(RawFlags.ToString("X8"));
            if (parts.Count > 0)
            {
                sb.Append(" [").Append(string.Join(" | ", parts.ToArray())).Append(']');
            }
            return sb.ToString();
        }
    }
}
