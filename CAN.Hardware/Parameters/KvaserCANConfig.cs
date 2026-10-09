using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class KvaserCANConfig : BusConfigBase, IXmlSerializer
    {
        private uint _canDeviceChannel = 0;
        private KvaserCanBitrate _canBitrate = KvaserCanBitrate.canBITRATE_500K;
        private int _tq = 8;
        private int _phase1 = 2;
        private int _phase2 = 2;
        private int _sjw = 1;
        private int _prop = 0;
        private int _prescale = 1;
        private bool _enableCustomBitrate = false;
        private bool _enableFilter = false;
        private uint _filter_Code = 0;
        private uint _filter_Mask = 0;
        private bool _fileter_IsExtended = false;
        [Description("CAN设备通道号")]
        public uint CanDeviceChannel
        {
            get => _canDeviceChannel;
            set => SetProperty(ref _canDeviceChannel, value);
        }
        [Description("CAN波特率")]
        public KvaserCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }
        [Description("总时间量子数")]
        public int Tq
        {
            get => _tq;
            set => SetProperty(ref _tq, value);
        }
        [Description("相位段1")]
        public int Phase1
        {
            get => _phase1;
            set => SetProperty(ref _phase1, value);
        }
        [Description("相位段2")]
        public int Phase2
        {
            get => _phase2;
            set => SetProperty(ref _phase2, value);
        }
        [Description("同步跳转宽度")]
        public int Sjw
        {
            get => _sjw;
            set => SetProperty(ref _sjw, value);
        }
        [Description("传播时间段")]
        public int Prop
        {
            get => _prop;
            set => SetProperty(ref _prop, value);
        }
        [Description("预分频值")]
        public int Prescale
        {
            get => _prescale;
            set => SetProperty(ref _prescale, value);
        }
        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }
        [Description("启用硬件过滤器")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }
        [Description("过滤器代码值")]
        public uint Filter_Code
        {
            get => _filter_Code;
            set => SetProperty(ref _filter_Code, value);
        }
        [Description("过滤器掩码值")]
        public uint Filter_Mask
        {
            get => _filter_Mask;
            set => SetProperty(ref _filter_Mask, value);
        }
        [Description("过滤器是否为扩展帧")]
        public bool Fileter_IsExtended
        {
            get => _fileter_IsExtended;
            set => SetProperty(ref _fileter_IsExtended, value);
        }
        public KvaserCANConfig() { }
        public void Init(XElement xElement)
        {
            CanDeviceChannel   = XmlAttributeParser.Get(xElement, nameof(CanDeviceChannel),   CanDeviceChannel,   throwIfMissing: false);
            CanBitrate         = XmlAttributeParser.Get(xElement, nameof(CanBitrate),         CanBitrate,         throwIfMissing: false);
            Tq                 = XmlAttributeParser.Get(xElement, nameof(Tq),                 Tq,                 throwIfMissing: false);
            Phase1             = XmlAttributeParser.Get(xElement, nameof(Phase1),             Phase1,             throwIfMissing: false);
            Phase2             = XmlAttributeParser.Get(xElement, nameof(Phase2),             Phase2,             throwIfMissing: false);
            Sjw                = XmlAttributeParser.Get(xElement, nameof(Sjw),                Sjw,                throwIfMissing: false);
            Prop               = XmlAttributeParser.Get(xElement, nameof(Prop),               Prop,               throwIfMissing: false);
            Prescale           = XmlAttributeParser.Get(xElement, nameof(Prescale),           Prescale,           throwIfMissing: false);
            EnableCustomBitrate= XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),EnableCustomBitrate,throwIfMissing: false);
            EnableFilter       = XmlAttributeParser.Get(xElement, nameof(EnableFilter),       EnableFilter,       throwIfMissing: false);
            Filter_Code        = XmlAttributeParser.Get(xElement, nameof(Filter_Code),        Filter_Code,        throwIfMissing: false);
            Filter_Mask        = XmlAttributeParser.Get(xElement, nameof(Filter_Mask),        Filter_Mask,        throwIfMissing: false);
            Fileter_IsExtended = XmlAttributeParser.Get(xElement, nameof(Fileter_IsExtended), Fileter_IsExtended, throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(CanDeviceChannel), CanDeviceChannel));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(Tq), Tq));
            xelement.Add(new XAttribute(nameof(Phase1), Phase1));
            xelement.Add(new XAttribute(nameof(Phase2), Phase2));
            xelement.Add(new XAttribute(nameof(Sjw), Sjw));
            xelement.Add(new XAttribute(nameof(Prop), Prop));
            xelement.Add(new XAttribute(nameof(Prescale), Prescale));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(Filter_Code), Filter_Code));
            xelement.Add(new XAttribute(nameof(Filter_Mask), Filter_Mask));
            xelement.Add(new XAttribute(nameof(Fileter_IsExtended), Fileter_IsExtended));
            return xelement;
        }
    }
}
