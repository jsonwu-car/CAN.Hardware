using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class VectorCanConfig : BusConfigBase, IXmlSerializer
    {
        private VectorHwType _hardwareType = VectorHwType.VN1630;
        private XLchannelInfo _channel = new XLchannelInfo();
        private VectorCanBitrate _canBitrate = VectorCanBitrate.canBITRATE_500K;
        private uint _customBitrate = 500000;
        private int _tseg1 = 13;
        private int _tseg2 = 2;
        private int _sjw = 1;
        private int _sam = 1;
        private bool _enableCustomBitrate = false;
        private bool _enableFilter = false;
        private uint _filter_Code = 0;
        private uint _filter_Mask = 0;
        private bool _fileter_IsExtended = false;

        [Description("Vector硬件类型")]
        public VectorHwType HardwareType
        {
            get => _hardwareType;
            set
            {
                if (SetProperty(ref _hardwareType, value))
                    OnPropertyChanged(nameof(AvailableChannels));
            }
        }
        [Description("CAN硬件通道(按驱动通道名选择)")]
        public XLchannelInfo Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        // 当前驱动中该硬件型号的可用通道列表(供界面下拉选择)
        public IEnumerable<XLchannelInfo> AvailableChannels => VectorCanLibDLL.GetCanChannels(HardwareType);

        [Description("CAN波特率")]
        public VectorCanBitrate CanBitrate
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
        [Description("时间段1")]
        public int Tseg1
        {
            get => _tseg1;
            set => SetProperty(ref _tseg1, value);
        }
        [Description("时间段2")]
        public int Tseg2
        {
            get => _tseg2;
            set => SetProperty(ref _tseg2, value);
        }
        [Description("同步跳转宽度")]
        public int Sjw
        {
            get => _sjw;
            set => SetProperty(ref _sjw, value);
        }
        [Description("采样次数(1或3)")]
        public int Sam
        {
            get => _sam;
            set => SetProperty(ref _sam, value);
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
        // Vector 过滤器使用 Code/Mask 掩码方式,与 Kvaser 一致
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
        public VectorCanConfig() { }
        public void Init(XElement xElement)
        {
            HardwareType       = XmlAttributeParser.Get(xElement, nameof(HardwareType),        HardwareType,         throwIfMissing: false);
            uint hwChannel     = XmlAttributeParser.Get(xElement, nameof(Channel),             Channel?.HwChannel ?? 0u, throwIfMissing: false);
            Channel            = new XLchannelInfo { HwChannel = hwChannel };
            CanBitrate         = XmlAttributeParser.Get(xElement, nameof(CanBitrate),          CanBitrate,           throwIfMissing: false);
            CustomBitrate      = XmlAttributeParser.Get(xElement, nameof(CustomBitrate),       CustomBitrate,        throwIfMissing: false);
            Tseg1              = XmlAttributeParser.Get(xElement, nameof(Tseg1),               Tseg1,                throwIfMissing: false);
            Tseg2              = XmlAttributeParser.Get(xElement, nameof(Tseg2),               Tseg2,                throwIfMissing: false);
            Sjw                = XmlAttributeParser.Get(xElement, nameof(Sjw),                 Sjw,                  throwIfMissing: false);
            Sam                = XmlAttributeParser.Get(xElement, nameof(Sam),                 Sam,                  throwIfMissing: false);
            EnableCustomBitrate= XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate), EnableCustomBitrate,  throwIfMissing: false);
            EnableFilter       = XmlAttributeParser.Get(xElement, nameof(EnableFilter),        EnableFilter,         throwIfMissing: false);
            Filter_Code        = XmlAttributeParser.Get(xElement, nameof(Filter_Code),         Filter_Code,          throwIfMissing: false);
            Filter_Mask        = XmlAttributeParser.Get(xElement, nameof(Filter_Mask),         Filter_Mask,          throwIfMissing: false);
            Fileter_IsExtended = XmlAttributeParser.Get(xElement, nameof(Fileter_IsExtended),  Fileter_IsExtended,   throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(HardwareType), HardwareType));
            xelement.Add(new XAttribute(nameof(Channel), Channel?.HwChannel ?? 0u));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(CustomBitrate), CustomBitrate));
            xelement.Add(new XAttribute(nameof(Tseg1), Tseg1));
            xelement.Add(new XAttribute(nameof(Tseg2), Tseg2));
            xelement.Add(new XAttribute(nameof(Sjw), Sjw));
            xelement.Add(new XAttribute(nameof(Sam), Sam));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(Filter_Code), Filter_Code));
            xelement.Add(new XAttribute(nameof(Filter_Mask), Filter_Mask));
            xelement.Add(new XAttribute(nameof(Fileter_IsExtended), Fileter_IsExtended));
            return xelement;
        }
    }
}
