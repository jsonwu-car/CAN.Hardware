using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// LIN 配置公共基类：请求延迟时间、总线波特率与节点模式。
    /// 各厂商差异项(校验方式/可变DLC/通道参数等)由派生类自行声明，
    /// 派生类重写 Init/ToXElement 时须调用 base 以保留公共属性的序列化。
    /// </summary>
    public abstract class LinConfigBase : BusConfigBase, IXmlSerializer
    {
        private uint _requestDelayTime = 10;
        private uint _linBitrate = 19200;
        private LinNodeMode _nodeMode = LinNodeMode.Master;

        [Description("请求延迟时间(毫秒),主节点等待从机应答的单次超时")]
        public uint RequestDelayTime
        {
            get => _requestDelayTime;
            set => SetProperty(ref _requestDelayTime, value);
        }

        [Description("LIN总线波特率(bps,1000~20000)")]
        public uint LinBitrate
        {
            get => _linBitrate;
            set => SetProperty(ref _linBitrate, value);
        }

        [Description("LIN节点模式(主/从/监听)")]
        public LinNodeMode NodeMode
        {
            get => _nodeMode;
            set => SetProperty(ref _nodeMode, value);
        }

        public virtual void Init(XElement xElement)
        {
            RequestDelayTime = XmlAttributeParser.Get(xElement, nameof(RequestDelayTime), RequestDelayTime, throwIfMissing: false);
            LinBitrate = XmlAttributeParser.Get(xElement, nameof(LinBitrate), LinBitrate, throwIfMissing: false);
            NodeMode = XmlAttributeParser.Get(xElement, nameof(NodeMode), NodeMode, throwIfMissing: false);
        }

        public virtual XElement ToXElement(string NodeName)
        {
            XElement xElement = new XElement(NodeName);
            xElement.Add(new XAttribute(nameof(RequestDelayTime), RequestDelayTime));
            xElement.Add(new XAttribute(nameof(LinBitrate), LinBitrate));
            xElement.Add(new XAttribute(nameof(NodeMode), NodeMode));
            return xElement;
        }
    }
}
