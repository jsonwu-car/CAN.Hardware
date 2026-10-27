using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class EtasCANConfig : BusConfigBase, IXmlSerializer
    {
        private int _channelIndex = 0;
        private string _interfaceUri = string.Empty;
        private EtasCanBitrate _canBitrate = EtasCanBitrate.etasBAUD_500K;

        [Description("CAN通道索引(0基,按CSI枚举到的CAN节点排序),仅InterfaceUri为空时生效")]
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

        [Description("CAN波特率")]
        public EtasCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }

        public EtasCANConfig() { }

        public void Init(XElement xElement)
        {
            ChannelIndex = XmlAttributeParser.Get(xElement, nameof(ChannelIndex), ChannelIndex, throwIfMissing: false);
            InterfaceUri = XmlAttributeParser.Get(xElement, nameof(InterfaceUri), InterfaceUri, throwIfMissing: false);
            CanBitrate   = XmlAttributeParser.Get(xElement, nameof(CanBitrate),   CanBitrate,   throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(ChannelIndex), ChannelIndex));
            xelement.Add(new XAttribute(nameof(InterfaceUri), InterfaceUri));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            return xelement;
        }
    }
}
