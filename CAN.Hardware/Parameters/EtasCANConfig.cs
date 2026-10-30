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
        private uint _customBitrate = 500000;
        private uint _samplePoint = 75;
        private uint _btlCycles = 10;
        private uint _sjw = 1;
        private bool _enableCustomBitrate = false;

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

        [Description("自定义波特率(bps)")]
        public uint CustomBitrate
        {
            get => _customBitrate;
            set => SetProperty(ref _customBitrate, value);
        }

        [Description("采样点(单位1%,75=75%)")]
        public uint SamplePoint
        {
            get => _samplePoint;
            set => SetProperty(ref _samplePoint, value);
        }

        [Description("每比特时间量子数(BTL_Cycles)")]
        public uint BtlCycles
        {
            get => _btlCycles;
            set => SetProperty(ref _btlCycles, value);
        }

        [Description("同步跳转宽度(SJW)")]
        public uint Sjw
        {
            get => _sjw;
            set => SetProperty(ref _sjw, value);
        }

        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        public EtasCANConfig() { }

        public void Init(XElement xElement)
        {
            ChannelIndex       = XmlAttributeParser.Get(xElement, nameof(ChannelIndex),        ChannelIndex,        throwIfMissing: false);
            InterfaceUri       = XmlAttributeParser.Get(xElement, nameof(InterfaceUri),        InterfaceUri,        throwIfMissing: false);
            CanBitrate         = XmlAttributeParser.Get(xElement, nameof(CanBitrate),          CanBitrate,          throwIfMissing: false);
            CustomBitrate      = XmlAttributeParser.Get(xElement, nameof(CustomBitrate),       CustomBitrate,       throwIfMissing: false);
            SamplePoint        = XmlAttributeParser.Get(xElement, nameof(SamplePoint),         SamplePoint,         throwIfMissing: false);
            BtlCycles          = XmlAttributeParser.Get(xElement, nameof(BtlCycles),           BtlCycles,           throwIfMissing: false);
            Sjw                = XmlAttributeParser.Get(xElement, nameof(Sjw),                 Sjw,                 throwIfMissing: false);
            EnableCustomBitrate= XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate), EnableCustomBitrate, throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(ChannelIndex), ChannelIndex));
            xelement.Add(new XAttribute(nameof(InterfaceUri), InterfaceUri));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(CustomBitrate), CustomBitrate));
            xelement.Add(new XAttribute(nameof(SamplePoint), SamplePoint));
            xelement.Add(new XAttribute(nameof(BtlCycles), BtlCycles));
            xelement.Add(new XAttribute(nameof(Sjw), Sjw));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            return xelement;
        }
    }
}
