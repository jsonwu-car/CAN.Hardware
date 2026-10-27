using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class EsdCANConfig : BusConfigBase, IXmlSerializer
    {
        private int _channel = 0;
        private EsdCanBitrate _canBitrate = EsdCanBitrate.esdBAUD_500K;
        private uint _customBitrate = 500000;
        private bool _enableCustomBitrate = false;

        [Description("CAN通道号(0基,直接传给 canOpen)")]
        public int Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        [Description("CAN波特率")]
        public EsdCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }

        [Description("自定义波特率(bps,EnableCustomBitrate=true 时生效)")]
        public uint CustomBitrate
        {
            get => _customBitrate;
            set => SetProperty(ref _customBitrate, value);
        }

        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        public EsdCANConfig() { }

        public void Init(XElement xElement)
        {
            Channel             = XmlAttributeParser.Get(xElement, nameof(Channel),             Channel,             throwIfMissing: false);
            CanBitrate          = XmlAttributeParser.Get(xElement, nameof(CanBitrate),          CanBitrate,          throwIfMissing: false);
            CustomBitrate       = XmlAttributeParser.Get(xElement, nameof(CustomBitrate),       CustomBitrate,       throwIfMissing: false);
            EnableCustomBitrate = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate), EnableCustomBitrate, throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(Channel), Channel));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(CustomBitrate), CustomBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            return xelement;
        }
    }
}
