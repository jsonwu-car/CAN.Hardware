using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class IntrepidCANConfig : BusConfigBase, IXmlSerializer
    {
        private uint _channel = 0;
        private IntrepidCanBitrate _canBitrate = IntrepidCanBitrate.icsBAUD_500K;
        private bool _enableCustomBitrate = false;
        private int _customBitrate = 500000;

        [Description("CAN通道号(0基,0~6 对应设备 HSCAN1~HSCAN7)")]
        public uint Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        [Description("CAN波特率")]
        public IntrepidCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }

        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        [Description("自定义波特率bps值(EnableCustomBitrate=true 时生效,驱动自动取最近值)")]
        public int CustomBitrate
        {
            get => _customBitrate;
            set => SetProperty(ref _customBitrate, value);
        }

        public IntrepidCANConfig() { }

        public void Init(XElement xElement)
        {
            Channel             = XmlAttributeParser.Get(xElement, nameof(Channel),             Channel,             throwIfMissing: false);
            CanBitrate          = XmlAttributeParser.Get(xElement, nameof(CanBitrate),          CanBitrate,          throwIfMissing: false);
            EnableCustomBitrate = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate), EnableCustomBitrate, throwIfMissing: false);
            CustomBitrate       = XmlAttributeParser.Get(xElement, nameof(CustomBitrate),       CustomBitrate,       throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(Channel), Channel));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(CustomBitrate), CustomBitrate));
            return xelement;
        }
    }
}
