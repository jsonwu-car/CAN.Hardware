using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class IntrepidCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private uint _channel = 0;
        private IntrepidCanBitrate _canFdArbitrationBitrate = IntrepidCanBitrate.icsBAUD_500K;
        private IntrepidCanFdBitrate _canFdDataBitrate = IntrepidCanFdBitrate.icsFDBAUD_2M;
        private bool _enableCustomBitrate = false;
        private int _customArbitrationBitrate = 500000;
        private int _customDataBitrate = 2000000;

        [Description("CAN FD通道号(0基,0~6 对应设备 HSCAN1~HSCAN7)")]
        public uint Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        [Description("仲裁段波特率")]
        public IntrepidCanBitrate CanFdArbitrationBitrate
        {
            get => _canFdArbitrationBitrate;
            set => SetProperty(ref _canFdArbitrationBitrate, value);
        }

        [Description("数据段波特率")]
        public IntrepidCanFdBitrate CanFdDataBitrate
        {
            get => _canFdDataBitrate;
            set => SetProperty(ref _canFdDataBitrate, value);
        }

        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        [Description("自定义仲裁段波特率bps值(EnableCustomBitrate=true 时生效)")]
        public int CustomArbitrationBitrate
        {
            get => _customArbitrationBitrate;
            set => SetProperty(ref _customArbitrationBitrate, value);
        }

        [Description("自定义数据段波特率bps值(EnableCustomBitrate=true 时生效)")]
        public int CustomDataBitrate
        {
            get => _customDataBitrate;
            set => SetProperty(ref _customDataBitrate, value);
        }

        public IntrepidCanFDConfig() { }

        public void Init(XElement xElement)
        {
            Channel                   = XmlAttributeParser.Get(xElement, nameof(Channel),                   Channel,                   throwIfMissing: false);
            CanFdArbitrationBitrate   = XmlAttributeParser.Get(xElement, nameof(CanFdArbitrationBitrate),   CanFdArbitrationBitrate,   throwIfMissing: false);
            CanFdDataBitrate          = XmlAttributeParser.Get(xElement, nameof(CanFdDataBitrate),          CanFdDataBitrate,          throwIfMissing: false);
            EnableCustomBitrate       = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),       EnableCustomBitrate,       throwIfMissing: false);
            CustomArbitrationBitrate  = XmlAttributeParser.Get(xElement, nameof(CustomArbitrationBitrate),  CustomArbitrationBitrate,  throwIfMissing: false);
            CustomDataBitrate         = XmlAttributeParser.Get(xElement, nameof(CustomDataBitrate),         CustomDataBitrate,         throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(Channel), Channel));
            xelement.Add(new XAttribute(nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CanFdDataBitrate), CanFdDataBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(CustomArbitrationBitrate), CustomArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CustomDataBitrate), CustomDataBitrate));
            return xelement;
        }
    }
}
