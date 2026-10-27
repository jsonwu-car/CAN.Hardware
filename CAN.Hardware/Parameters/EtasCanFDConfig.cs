using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class EtasCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private int _channelIndex = 0;
        private string _interfaceUri = string.Empty;
        private EtasCanBitrate _canFdArbitrationBitrate = EtasCanBitrate.etasBAUD_500K;
        private EtasCanFdBitrate _canFdDataBitrate = EtasCanFdBitrate.etasFDBAUD_2M;

        [Description("CANFD通道索引(0基,按CSI枚举到的CAN节点排序),仅InterfaceUri为空时生效")]
        public int ChannelIndex
        {
            get => _channelIndex;
            set => SetProperty(ref _channelIndex, value);
        }

        [Description("BOA硬件URI(如/ETAS.ETH://ES581.4/CAN:1),非空时直接使用,跳过CSI枚举")]
        public string InterfaceUri
        {
            get => _interfaceUri;
            set => SetProperty(ref _interfaceUri, value);
        }

        [Description("CANFD仲裁段波特率")]
        public EtasCanBitrate CanFdArbitrationBitrate
        {
            get => _canFdArbitrationBitrate;
            set => SetProperty(ref _canFdArbitrationBitrate, value);
        }

        [Description("CANFD数据段波特率")]
        public EtasCanFdBitrate CanFdDataBitrate
        {
            get => _canFdDataBitrate;
            set => SetProperty(ref _canFdDataBitrate, value);
        }

        public EtasCanFDConfig() { }

        public void Init(XElement xElement)
        {
            ChannelIndex            = XmlAttributeParser.Get(xElement, nameof(ChannelIndex),            ChannelIndex,            throwIfMissing: false);
            InterfaceUri            = XmlAttributeParser.Get(xElement, nameof(InterfaceUri),            InterfaceUri,            throwIfMissing: false);
            CanFdArbitrationBitrate = XmlAttributeParser.Get(xElement, nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate, throwIfMissing: false);
            CanFdDataBitrate        = XmlAttributeParser.Get(xElement, nameof(CanFdDataBitrate),        CanFdDataBitrate,        throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(ChannelIndex), ChannelIndex));
            xelement.Add(new XAttribute(nameof(InterfaceUri), InterfaceUri));
            xelement.Add(new XAttribute(nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CanFdDataBitrate), CanFdDataBitrate));
            return xelement;
        }
    }
}
