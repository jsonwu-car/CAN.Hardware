using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class VectorCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private VectorHwType _hardwareType = VectorHwType.VN1630;
        private XLchannelInfo _channel = new XLchannelInfo();
        private VectorCanBitrate _canFdArbitrationBitrate = VectorCanBitrate.canBITRATE_500K;
        private VectorCanFdBitrate _canFdDataBitrate = VectorCanFdBitrate.canFD_BITRATE_2M;
        private uint _customArbitrationBitrate = 500000;
        private int _arbitration_tseg1 = 119;
        private int _arbitration_tseg2 = 40;
        private int _arbitration_sjw = 40;
        private uint _customDataBitrate = 2000000;
        private int _data_tseg1 = 29;
        private int _data_tseg2 = 10;
        private int _data_sjw = 10;
        private bool _enableNonIso = false;
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
        [Description("CAN FD硬件通道(按驱动通道名选择)")]
        public XLchannelInfo Channel
        {
            get => _channel;
            set => SetProperty(ref _channel, value);
        }

        // 当前驱动中该硬件型号的可用通道列表(供界面下拉选择)
        public IEnumerable<XLchannelInfo> AvailableChannels => VectorCanLibDLL.GetCanChannels(HardwareType);

        [Description("仲裁段波特率")]
        public VectorCanBitrate CanFdArbitrationBitrate
        {
            get => _canFdArbitrationBitrate;
            set => SetProperty(ref _canFdArbitrationBitrate, value);
        }
        [Description("数据段波特率")]
        public VectorCanFdBitrate CanFdDataBitrate
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
        [Description("仲裁段时间段1")]
        public int Arbitration_tseg1
        {
            get => _arbitration_tseg1;
            set => SetProperty(ref _arbitration_tseg1, value);
        }
        [Description("仲裁段时间段2")]
        public int Arbitration_tseg2
        {
            get => _arbitration_tseg2;
            set => SetProperty(ref _arbitration_tseg2, value);
        }
        [Description("仲裁段同步跳转宽度")]
        public int Arbitration_sjw
        {
            get => _arbitration_sjw;
            set => SetProperty(ref _arbitration_sjw, value);
        }
        [Description("数据段自定义波特率(bps)")]
        public uint CustomDataBitrate
        {
            get => _customDataBitrate;
            set => SetProperty(ref _customDataBitrate, value);
        }
        [Description("数据段时间段1")]
        public int Data_tseg1
        {
            get => _data_tseg1;
            set => SetProperty(ref _data_tseg1, value);
        }
        [Description("数据段时间段2")]
        public int Data_tseg2
        {
            get => _data_tseg2;
            set => SetProperty(ref _data_tseg2, value);
        }
        [Description("数据段同步跳转宽度")]
        public int Data_sjw
        {
            get => _data_sjw;
            set => SetProperty(ref _data_sjw, value);
        }
        [Description("启用Non-ISO(CANFD-BOSCH)")]
        public bool EnableNonIso
        {
            get => _enableNonIso;
            set => SetProperty(ref _enableNonIso, value);
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
        public VectorCanFDConfig() { }
        public void Init(XElement xElement)
        {
            HardwareType            = XmlAttributeParser.Get(xElement, nameof(HardwareType),             HardwareType,            throwIfMissing: false);
            uint hwChannel          = XmlAttributeParser.Get(xElement, nameof(Channel),                  Channel?.HwChannel ?? 0u, throwIfMissing: false);
            Channel                 = new XLchannelInfo { HwChannel = hwChannel };
            CanFdArbitrationBitrate = XmlAttributeParser.Get(xElement, nameof(CanFdArbitrationBitrate),  CanFdArbitrationBitrate, throwIfMissing: false);
            CanFdDataBitrate        = XmlAttributeParser.Get(xElement, nameof(CanFdDataBitrate),         CanFdDataBitrate,        throwIfMissing: false);
            CustomArbitrationBitrate= XmlAttributeParser.Get(xElement, nameof(CustomArbitrationBitrate), CustomArbitrationBitrate,throwIfMissing: false);
            Arbitration_tseg1       = XmlAttributeParser.Get(xElement, nameof(Arbitration_tseg1),        Arbitration_tseg1,       throwIfMissing: false);
            Arbitration_tseg2       = XmlAttributeParser.Get(xElement, nameof(Arbitration_tseg2),        Arbitration_tseg2,       throwIfMissing: false);
            Arbitration_sjw         = XmlAttributeParser.Get(xElement, nameof(Arbitration_sjw),          Arbitration_sjw,         throwIfMissing: false);
            CustomDataBitrate       = XmlAttributeParser.Get(xElement, nameof(CustomDataBitrate),        CustomDataBitrate,       throwIfMissing: false);
            Data_tseg1              = XmlAttributeParser.Get(xElement, nameof(Data_tseg1),               Data_tseg1,              throwIfMissing: false);
            Data_tseg2              = XmlAttributeParser.Get(xElement, nameof(Data_tseg2),               Data_tseg2,              throwIfMissing: false);
            Data_sjw                = XmlAttributeParser.Get(xElement, nameof(Data_sjw),                 Data_sjw,                throwIfMissing: false);
            EnableNonIso            = XmlAttributeParser.Get(xElement, nameof(EnableNonIso),             EnableNonIso,            throwIfMissing: false);
            EnableCustomBitrate     = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),      EnableCustomBitrate,     throwIfMissing: false);
            EnableFilter            = XmlAttributeParser.Get(xElement, nameof(EnableFilter),             EnableFilter,            throwIfMissing: false);
            Filter_Code             = XmlAttributeParser.Get(xElement, nameof(Filter_Code),              Filter_Code,             throwIfMissing: false);
            Filter_Mask             = XmlAttributeParser.Get(xElement, nameof(Filter_Mask),              Filter_Mask,             throwIfMissing: false);
            Fileter_IsExtended      = XmlAttributeParser.Get(xElement, nameof(Fileter_IsExtended),       Fileter_IsExtended,      throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(HardwareType), HardwareType));
            xelement.Add(new XAttribute(nameof(Channel), Channel?.HwChannel ?? 0u));
            xelement.Add(new XAttribute(nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CanFdDataBitrate), CanFdDataBitrate));
            xelement.Add(new XAttribute(nameof(CustomArbitrationBitrate), CustomArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(Arbitration_tseg1), Arbitration_tseg1));
            xelement.Add(new XAttribute(nameof(Arbitration_tseg2), Arbitration_tseg2));
            xelement.Add(new XAttribute(nameof(Arbitration_sjw), Arbitration_sjw));
            xelement.Add(new XAttribute(nameof(CustomDataBitrate), CustomDataBitrate));
            xelement.Add(new XAttribute(nameof(Data_tseg1), Data_tseg1));
            xelement.Add(new XAttribute(nameof(Data_tseg2), Data_tseg2));
            xelement.Add(new XAttribute(nameof(Data_sjw), Data_sjw));
            xelement.Add(new XAttribute(nameof(EnableNonIso), EnableNonIso));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(Filter_Code), Filter_Code));
            xelement.Add(new XAttribute(nameof(Filter_Mask), Filter_Mask));
            xelement.Add(new XAttribute(nameof(Fileter_IsExtended), Fileter_IsExtended));
            return xelement;
        }
    }
}
