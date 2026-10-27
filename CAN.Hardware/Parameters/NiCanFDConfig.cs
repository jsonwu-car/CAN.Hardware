using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class NiCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private string _interfaceName = "CAN1";
        private NiCanBitrate _canFdArbitrationBitrate = NiCanBitrate.niBAUD_500K;
        private NiCanFdBitrate _canFdDataBitrate = NiCanFdBitrate.niFDBAUD_2M;

        [Description("NI-XNET接口名(如CAN1,可在NI MAX中查看/重命名)")]
        public string InterfaceName
        {
            get => _interfaceName;
            set => SetProperty(ref _interfaceName, value);
        }

        [Description("CANFD仲裁段波特率")]
        public NiCanBitrate CanFdArbitrationBitrate
        {
            get => _canFdArbitrationBitrate;
            set => SetProperty(ref _canFdArbitrationBitrate, value);
        }

        [Description("CANFD数据段波特率")]
        public NiCanFdBitrate CanFdDataBitrate
        {
            get => _canFdDataBitrate;
            set => SetProperty(ref _canFdDataBitrate, value);
        }

        public NiCanFDConfig() { }

        public void Init(XElement xElement)
        {
            InterfaceName           = XmlAttributeParser.Get(xElement, nameof(InterfaceName),           InterfaceName,           throwIfMissing: false);
            CanFdArbitrationBitrate = XmlAttributeParser.Get(xElement, nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate, throwIfMissing: false);
            CanFdDataBitrate        = XmlAttributeParser.Get(xElement, nameof(CanFdDataBitrate),        CanFdDataBitrate,        throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(InterfaceName), InterfaceName));
            xelement.Add(new XAttribute(nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CanFdDataBitrate), CanFdDataBitrate));
            return xelement;
        }
    }
}
