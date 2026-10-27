using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class NiCANConfig : BusConfigBase, IXmlSerializer
    {
        private string _interfaceName = "CAN1";
        private NiCanBitrate _canBitrate = NiCanBitrate.niBAUD_500K;

        [Description("NI-XNET接口名(如CAN1,可在NI MAX中查看/重命名)")]
        public string InterfaceName
        {
            get => _interfaceName;
            set => SetProperty(ref _interfaceName, value);
        }

        [Description("CAN波特率")]
        public NiCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }

        public NiCANConfig() { }

        public void Init(XElement xElement)
        {
            InterfaceName = XmlAttributeParser.Get(xElement, nameof(InterfaceName), InterfaceName, throwIfMissing: false);
            CanBitrate    = XmlAttributeParser.Get(xElement, nameof(CanBitrate),    CanBitrate,    throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(InterfaceName), InterfaceName));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            return xelement;
        }
    }
}
