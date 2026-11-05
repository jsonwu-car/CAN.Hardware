using System.Collections.Generic;
using System.Text;

namespace CAN.Hardware.Interface
{
    /// <summary>
    /// LIN 总线帧解析后的标志位信息。
    /// 同一总线不同厂商的解析结果一致,由厂商各自的解析器填充。
    /// </summary>
    public class LinFrameFlags : BusFrameFlags
    {
        /// <summary>发送方向(本机发出/回显)</summary>
        public bool IsTx { get; internal set; }
        /// <summary>接收方向</summary>
        public bool IsRx { get; internal set; }
        /// <summary>发送间隔场(Break)</summary>
        public bool IsSendBreak { get; internal set; }
        /// <summary>接收到间隔场(Break)</summary>
        public bool IsReceivedBreak { get; internal set; }
        /// <summary>接收到同步场</summary>
        public bool IsReceivedSync { get; internal set; }
        /// <summary>厂商原始错误状态(0 表示无错误,具体含义见各厂商文档)</summary>
        public byte ErrorStatus { get; internal set; }

        /// <summary>
        /// 接收时解析出的报文附加信息(波特率/校验和/时间参数等)。
        /// Send 路径不填充,为 null。
        /// </summary>
        public LinMessageInfoData? MessageInfo { get; internal set; }

        public override bool IsError
        {
            get { return ErrorStatus != 0; }
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (IsTx) parts.Add("TX");
            if (IsRx) parts.Add("RX");
            if (IsSendBreak) parts.Add("SEND_BREAK");
            if (IsReceivedBreak) parts.Add("RECV_BREAK");
            if (IsReceivedSync) parts.Add("RECV_SYNC");
            if (IsError) parts.Add("ERROR(0x" + ErrorStatus.ToString("X2") + ")");

            var sb = new StringBuilder();
            sb.Append("0x").Append(RawFlags.ToString("X8"));
            if (parts.Count > 0)
            {
                sb.Append(" [").Append(string.Join(" | ", parts.ToArray())).Append(']');
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// LIN 报文附加信息(对应 Kvaser linlib 的 LinMessageInfo)。
    /// </summary>
    public class LinMessageInfoData
    {
        /// <summary>同步间隔场长度</summary>
        public uint SynchBreakLength { get; internal set; }
        /// <summary>帧长度</summary>
        public uint FrameLength { get; internal set; }
        /// <summary>总线波特率</summary>
        public uint Bitrate { get; internal set; }
        /// <summary>校验和</summary>
        public byte CheckSum { get; internal set; }
        /// <summary>带奇偶校验位的标识符</summary>
        public byte IdPar { get; internal set; }
        /// <summary>同步边沿时间(4 个)</summary>
        public uint[] SynchEdgeTimes { get; internal set; } = new uint[4];
        /// <summary>各字节时间(8 个)</summary>
        public uint[] ByteTimes { get; internal set; } = new uint[8];
    }
}
