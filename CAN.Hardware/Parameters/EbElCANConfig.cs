using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class EbElCANConfig : BusConfigBase, IXmlSerializer
    {
        private uint _channel = 0;
        private EbElCanBitrate _canBitrate = EbElCanBitrate.fcBAUD_500K;

        [Description("CAN通道号(0基,0~7 对应设备 CC1~CC8)")]
        public uint Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        [Description("CAN波特率(fcbase 无数值接口,按位时序表配置)")]
        public EbElCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }

        public EbElCANConfig() { }

        public void Init(XElement xElement)
        {
            Channel    = XmlAttributeParser.Get(xElement, nameof(Channel),    Channel,    throwIfMissing: false);
            CanBitrate = XmlAttributeParser.Get(xElement, nameof(CanBitrate), CanBitrate, throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(Channel), Channel));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            return xelement;
        }
    }
}
