using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.BaseLibDll;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 同星(Tosun) LIN 配置。
    /// 通道索引与 LIN 协议版本直接映射 libTSCAN 的枚举;协议版本决定校验和类型:
    /// LIN 1.3 / J2602 为经典校验,LIN 2.0 / 2.1 为增强校验。
    /// </summary>
    public class ToSunLINConfig : LinConfigBase
    {
        private bool _enableFIFO = true;
        private bool _enableErrorFrame = true;
        private bool _enableTurbe = false;
        private CHANNEL_INDEX _channelIndex = CHANNEL_INDEX.CHN1;
        private LIN_PROTOCOL _linProtocol = LIN_PROTOCOL.LIN_PROTOCOL_20;

        [Description("启用FIFO模式")]
        public bool EnableFIFO
        {
            get => _enableFIFO;
            set => SetProperty(ref _enableFIFO, value);
        }

        [Description("启用错误帧接收")]
        public bool EnableErrorFrame
        {
            get => _enableErrorFrame;
            set => SetProperty(ref _enableErrorFrame, value);
        }

        [Description("启用Turbo模式")]
        public bool EnableTurbe
        {
            get => _enableTurbe;
            set => SetProperty(ref _enableTurbe, value);
        }

        [Description("LIN通道索引(0基,CHN1=0)")]
        public CHANNEL_INDEX ChannelIndex
        {
            get => _channelIndex;
            set => SetProperty(ref _channelIndex, value);
        }

        [Description("LIN协议版本(决定校验方式:1.3/J2602=经典,2.x=增强)")]
        public LIN_PROTOCOL LinProtocol
        {
            get => _linProtocol;
            set => SetProperty(ref _linProtocol, value);
        }

        public override void Init(XElement xElement)
        {
            base.Init(xElement);
            EnableFIFO = XmlAttributeParser.Get(xElement, nameof(EnableFIFO), EnableFIFO, throwIfMissing: false);
            EnableErrorFrame = XmlAttributeParser.Get(xElement, nameof(EnableErrorFrame), EnableErrorFrame, throwIfMissing: false);
            EnableTurbe = XmlAttributeParser.Get(xElement, nameof(EnableTurbe), EnableTurbe, throwIfMissing: false);
            ChannelIndex = XmlAttributeParser.Get(xElement, nameof(ChannelIndex), ChannelIndex, throwIfMissing: false);
            LinProtocol = XmlAttributeParser.Get(xElement, nameof(LinProtocol), LinProtocol, throwIfMissing: false);
        }

        public override XElement ToXElement(string NodeName)
        {
            XElement xElement = base.ToXElement(NodeName);
            xElement.Add(new XAttribute(nameof(EnableFIFO), EnableFIFO));
            xElement.Add(new XAttribute(nameof(EnableErrorFrame), EnableErrorFrame));
            xElement.Add(new XAttribute(nameof(EnableTurbe), EnableTurbe));
            xElement.Add(new XAttribute(nameof(ChannelIndex), ChannelIndex));
            xElement.Add(new XAttribute(nameof(LinProtocol), LinProtocol));
            return xElement;
        }
    }
}
