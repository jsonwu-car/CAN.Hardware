using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class EsdCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private int _channel = 0;
        private EsdCanBitrate _canFdArbitrationBitrate = EsdCanBitrate.esdBAUD_500K;
        private EsdCanFdBitrate _canFdDataBitrate = EsdCanFdBitrate.esdBAUD_2M;
        private uint _customArbitrationBitrate = 500000;
        private uint _customDataBitrate = 2000000;
        private bool _enableCustomBitrate = false;

        [Description("CAN FD通道号(0基,直接传给 canOpen)")]
        public int Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        [Description("仲裁段波特率")]
        public EsdCanBitrate CanFdArbitrationBitrate
        {
            get => _canFdArbitrationBitrate;
            set => SetProperty(ref _canFdArbitrationBitrate, value);
        }

        [Description("数据段波特率")]
        public EsdCanFdBitrate CanFdDataBitrate
        {
            get => _canFdDataBitrate;
            set => SetProperty(ref _canFdDataBitrate, value);
        }

        [Description("自定义仲裁段波特率(bps,EnableCustomBitrate=true 时生效)")]
        public uint CustomArbitrationBitrate
        {
            get => _customArbitrationBitrate;
            set => SetProperty(ref _customArbitrationBitrate, value);
        }

        [Description("自定义数据段波特率(bps,EnableCustomBitrate=true 时生效)")]
        public uint CustomDataBitrate
        {
            get => _customDataBitrate;
            set => SetProperty(ref _customDataBitrate, value);
        }

        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        public EsdCanFDConfig() { }

        public void Init(XElement xElement)
        {
            Channel                   = XmlAttributeParser.Get(xElement, nameof(Channel),                   Channel,                   throwIfMissing: false);
            CanFdArbitrationBitrate   = XmlAttributeParser.Get(xElement, nameof(CanFdArbitrationBitrate),   CanFdArbitrationBitrate,   throwIfMissing: false);
            CanFdDataBitrate          = XmlAttributeParser.Get(xElement, nameof(CanFdDataBitrate),          CanFdDataBitrate,          throwIfMissing: false);
            CustomArbitrationBitrate  = XmlAttributeParser.Get(xElement, nameof(CustomArbitrationBitrate),  CustomArbitrationBitrate,  throwIfMissing: false);
            CustomDataBitrate         = XmlAttributeParser.Get(xElement, nameof(CustomDataBitrate),         CustomDataBitrate,         throwIfMissing: false);
            EnableCustomBitrate       = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),       EnableCustomBitrate,       throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(Channel), Channel));
            xelement.Add(new XAttribute(nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CanFdDataBitrate), CanFdDataBitrate));
            xelement.Add(new XAttribute(nameof(CustomArbitrationBitrate), CustomArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CustomDataBitrate), CustomDataBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            return xelement;
        }
    }
}
