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
        private uint _customArbitrationBitrate = 500000;
        private uint _arbitrationSamplePoint = 75;
        private uint _arbitrationBtlCycles = 10;
        private uint _arbitrationSjw = 1;
        private uint _customDataBitrate = 2000000;
        private uint _dataSamplePoint = 80;
        private uint _dataBtlCycles = 10;
        private uint _dataSjw = 1;
        private bool _enableCustomBitrate = false;

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

        [Description("仲裁段自定义波特率(bps)")]
        public uint CustomArbitrationBitrate
        {
            get => _customArbitrationBitrate;
            set => SetProperty(ref _customArbitrationBitrate, value);
        }

        [Description("仲裁段采样点(单位1%,75=75%)")]
        public uint ArbitrationSamplePoint
        {
            get => _arbitrationSamplePoint;
            set => SetProperty(ref _arbitrationSamplePoint, value);
        }

        [Description("仲裁段每比特时间量子数(BTL_Cycles)")]
        public uint ArbitrationBtlCycles
        {
            get => _arbitrationBtlCycles;
            set => SetProperty(ref _arbitrationBtlCycles, value);
        }

        [Description("仲裁段同步跳转宽度(SJW)")]
        public uint ArbitrationSjw
        {
            get => _arbitrationSjw;
            set => SetProperty(ref _arbitrationSjw, value);
        }

        [Description("数据段自定义波特率(bps)")]
        public uint CustomDataBitrate
        {
            get => _customDataBitrate;
            set => SetProperty(ref _customDataBitrate, value);
        }

        [Description("数据段采样点(单位1%,80=80%)")]
        public uint DataSamplePoint
        {
            get => _dataSamplePoint;
            set => SetProperty(ref _dataSamplePoint, value);
        }

        [Description("数据段每比特时间量子数(BTL_Cycles)")]
        public uint DataBtlCycles
        {
            get => _dataBtlCycles;
            set => SetProperty(ref _dataBtlCycles, value);
        }

        [Description("数据段同步跳转宽度(SJW)")]
        public uint DataSjw
        {
            get => _dataSjw;
            set => SetProperty(ref _dataSjw, value);
        }

        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        public EtasCanFDConfig() { }

        public void Init(XElement xElement)
        {
            ChannelIndex             = XmlAttributeParser.Get(xElement, nameof(ChannelIndex),              ChannelIndex,              throwIfMissing: false);
            InterfaceUri             = XmlAttributeParser.Get(xElement, nameof(InterfaceUri),              InterfaceUri,              throwIfMissing: false);
            CanFdArbitrationBitrate  = XmlAttributeParser.Get(xElement, nameof(CanFdArbitrationBitrate),   CanFdArbitrationBitrate,   throwIfMissing: false);
            CanFdDataBitrate         = XmlAttributeParser.Get(xElement, nameof(CanFdDataBitrate),          CanFdDataBitrate,          throwIfMissing: false);
            CustomArbitrationBitrate = XmlAttributeParser.Get(xElement, nameof(CustomArbitrationBitrate),  CustomArbitrationBitrate,  throwIfMissing: false);
            ArbitrationSamplePoint   = XmlAttributeParser.Get(xElement, nameof(ArbitrationSamplePoint),    ArbitrationSamplePoint,    throwIfMissing: false);
            ArbitrationBtlCycles     = XmlAttributeParser.Get(xElement, nameof(ArbitrationBtlCycles),      ArbitrationBtlCycles,      throwIfMissing: false);
            ArbitrationSjw           = XmlAttributeParser.Get(xElement, nameof(ArbitrationSjw),            ArbitrationSjw,            throwIfMissing: false);
            CustomDataBitrate        = XmlAttributeParser.Get(xElement, nameof(CustomDataBitrate),         CustomDataBitrate,         throwIfMissing: false);
            DataSamplePoint          = XmlAttributeParser.Get(xElement, nameof(DataSamplePoint),           DataSamplePoint,           throwIfMissing: false);
            DataBtlCycles            = XmlAttributeParser.Get(xElement, nameof(DataBtlCycles),             DataBtlCycles,             throwIfMissing: false);
            DataSjw                  = XmlAttributeParser.Get(xElement, nameof(DataSjw),                   DataSjw,                   throwIfMissing: false);
            EnableCustomBitrate      = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),       EnableCustomBitrate,       throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(ChannelIndex), ChannelIndex));
            xelement.Add(new XAttribute(nameof(InterfaceUri), InterfaceUri));
            xelement.Add(new XAttribute(nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CanFdDataBitrate), CanFdDataBitrate));
            xelement.Add(new XAttribute(nameof(CustomArbitrationBitrate), CustomArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(ArbitrationSamplePoint), ArbitrationSamplePoint));
            xelement.Add(new XAttribute(nameof(ArbitrationBtlCycles), ArbitrationBtlCycles));
            xelement.Add(new XAttribute(nameof(ArbitrationSjw), ArbitrationSjw));
            xelement.Add(new XAttribute(nameof(CustomDataBitrate), CustomDataBitrate));
            xelement.Add(new XAttribute(nameof(DataSamplePoint), DataSamplePoint));
            xelement.Add(new XAttribute(nameof(DataBtlCycles), DataBtlCycles));
            xelement.Add(new XAttribute(nameof(DataSjw), DataSjw));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            return xelement;
        }
    }
}
