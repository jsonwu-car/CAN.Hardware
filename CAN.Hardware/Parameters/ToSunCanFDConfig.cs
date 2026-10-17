using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class ToSunCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private CHANNEL_INDEX _channelIndex = CHANNEL_INDEX.CHN1;
        private double _canFdArbitrationBitrate = 500;
        private double _canFdDataBitrate = 2000;
        private TLIBCANFDControllerType _controllerType = TLIBCANFDControllerType.lfdtISOCANFD;
        private TLIBCANFDControllerMode _controllerMode = TLIBCANFDControllerMode.lfdmNormal;
        private bool _enableA120Ohm = true;
        private bool _enableFIFO = true;
        private bool _enableErrorFrame = true;
        private bool _enableTurbe = false;
        private bool _enableCustomBitrate = false;
        private int _arbitration_phase1 = 8;
        private int _arbitration_phase2 = 8;
        private int _arbitration_sjw = 8;
        private int _arbitration_prescale = 1;
        private int _data_phase1 = 8;
        private int _data_phase2 = 4;
        private int _data_sjw = 4;
        private int _data_prescale = 1;
        private bool _enableFilter = false;
        private List<uint>? _filterIDArray = null;
        [Description("CAN FD通道索引(0基,CHN1=0)")]
        public CHANNEL_INDEX ChannelIndex
        {
            get => _channelIndex;
            set => SetProperty(ref _channelIndex, value);
        }
        [Description("仲裁段波特率(Kbps)")]
        public double CanFdArbitrationBitrate
        {
            get => _canFdArbitrationBitrate;
            set => SetProperty(ref _canFdArbitrationBitrate, value);
        }
        [Description("数据段波特率(Kbps)")]
        public double CanFdDataBitrate
        {
            get => _canFdDataBitrate;
            set => SetProperty(ref _canFdDataBitrate, value);
        }
        [Description("CAN FD控制器类型")]
        public TLIBCANFDControllerType ControllerType
        {
            get => _controllerType;
            set => SetProperty(ref _controllerType, value);
        }
        [Description("CAN FD控制器模式")]
        public TLIBCANFDControllerMode ControllerMode
        {
            get => _controllerMode;
            set => SetProperty(ref _controllerMode, value);
        }
        [Description("启用120欧姆终端电阻")]
        public bool EnableA120Ohm
        {
            get => _enableA120Ohm;
            set => SetProperty(ref _enableA120Ohm, value);
        }
        [Description("启用FIFO模式")]
        public bool EnableFIFO
        {
            get => _enableFIFO;
            set => SetProperty(ref _enableFIFO, value);
        }
        [Description("启用错误帧接收")]
        public bool EnableErrorFrame
        {
            get => _enableErrorFrame;
            set => SetProperty(ref _enableErrorFrame, value);
        }
        [Description("启用Turbo模式")]
        public bool EnableTurbe
        {
            get => _enableTurbe;
            set => SetProperty(ref _enableTurbe, value);
        }
        [Description("启用自定义波特率(寄存器级配置)")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }
        [Description("仲裁段相位段1")]
        public int Arbitration_phase1
        {
            get => _arbitration_phase1;
            set => SetProperty(ref _arbitration_phase1, value);
        }
        [Description("仲裁段相位段2")]
        public int Arbitration_phase2
        {
            get => _arbitration_phase2;
            set => SetProperty(ref _arbitration_phase2, value);
        }
        [Description("仲裁段同步跳转宽度")]
        public int Arbitration_sjw
        {
            get => _arbitration_sjw;
            set => SetProperty(ref _arbitration_sjw, value);
        }
        [Description("仲裁段预分频值")]
        public int Arbitration_prescale
        {
            get => _arbitration_prescale;
            set => SetProperty(ref _arbitration_prescale, value);
        }
        [Description("数据段相位段1")]
        public int Data_phase1
        {
            get => _data_phase1;
            set => SetProperty(ref _data_phase1, value);
        }
        [Description("数据段相位段2")]
        public int Data_phase2
        {
            get => _data_phase2;
            set => SetProperty(ref _data_phase2, value);
        }
        [Description("数据段同步跳转宽度")]
        public int Data_sjw
        {
            get => _data_sjw;
            set => SetProperty(ref _data_sjw, value);
        }
        [Description("数据段预分频值")]
        public int Data_prescale
        {
            get => _data_prescale;
            set => SetProperty(ref _data_prescale, value);
        }
        [Description("启用硬件过滤器")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }
        // ToSun 过滤器为逐 ID 白名单模式(tsfifo_add_can_canfd_pass_filter),而非掩码方式
        [Description("过滤器ID列表(白名单)")]
        public List<uint>? FilterIDArray
        {
            get => _filterIDArray;
            set => SetProperty(ref _filterIDArray, value);
        }
        public ToSunCanFDConfig() { }
        public void Init(XElement xElement)
        {
            ChannelIndex            = XmlAttributeParser.Get(xElement, nameof(ChannelIndex),            ChannelIndex,            throwIfMissing: false);
            CanFdArbitrationBitrate = XmlAttributeParser.Get(xElement, nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate, throwIfMissing: false);
            CanFdDataBitrate        = XmlAttributeParser.Get(xElement, nameof(CanFdDataBitrate),        CanFdDataBitrate,        throwIfMissing: false);
            ControllerType          = XmlAttributeParser.Get(xElement, nameof(ControllerType),          ControllerType,          throwIfMissing: false);
            ControllerMode          = XmlAttributeParser.Get(xElement, nameof(ControllerMode),          ControllerMode,          throwIfMissing: false);
            EnableA120Ohm           = XmlAttributeParser.Get(xElement, nameof(EnableA120Ohm),           EnableA120Ohm,           throwIfMissing: false);
            EnableFIFO              = XmlAttributeParser.Get(xElement, nameof(EnableFIFO),              EnableFIFO,              throwIfMissing: false);
            EnableErrorFrame        = XmlAttributeParser.Get(xElement, nameof(EnableErrorFrame),        EnableErrorFrame,        throwIfMissing: false);
            EnableTurbe             = XmlAttributeParser.Get(xElement, nameof(EnableTurbe),             EnableTurbe,             throwIfMissing: false);
            EnableCustomBitrate     = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),     EnableCustomBitrate,     throwIfMissing: false);
            Arbitration_phase1      = XmlAttributeParser.Get(xElement, nameof(Arbitration_phase1),      Arbitration_phase1,      throwIfMissing: false);
            Arbitration_phase2      = XmlAttributeParser.Get(xElement, nameof(Arbitration_phase2),      Arbitration_phase2,      throwIfMissing: false);
            Arbitration_sjw         = XmlAttributeParser.Get(xElement, nameof(Arbitration_sjw),         Arbitration_sjw,         throwIfMissing: false);
            Arbitration_prescale    = XmlAttributeParser.Get(xElement, nameof(Arbitration_prescale),    Arbitration_prescale,    throwIfMissing: false);
            Data_phase1             = XmlAttributeParser.Get(xElement, nameof(Data_phase1),             Data_phase1,             throwIfMissing: false);
            Data_phase2             = XmlAttributeParser.Get(xElement, nameof(Data_phase2),             Data_phase2,             throwIfMissing: false);
            Data_sjw                = XmlAttributeParser.Get(xElement, nameof(Data_sjw),                Data_sjw,                throwIfMissing: false);
            Data_prescale           = XmlAttributeParser.Get(xElement, nameof(Data_prescale),           Data_prescale,           throwIfMissing: false);
            EnableFilter            = XmlAttributeParser.Get(xElement, nameof(EnableFilter),            EnableFilter,            throwIfMissing: false);

            // FilterIDArray 为 List<uint>,XmlAttributeParser 不支持,按子元素+ID_<值>属性手写解析
            XElement? filterElement = xElement.Element(nameof(FilterIDArray));
            if (filterElement != null)
            {
                List<uint> filters = new List<uint>();
                foreach (XAttribute attribute in filterElement.Attributes())
                {
                    filters.Add(uint.Parse(attribute.Value));
                }
                FilterIDArray = filters;
            }
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(ChannelIndex), ChannelIndex));
            xelement.Add(new XAttribute(nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CanFdDataBitrate), CanFdDataBitrate));
            xelement.Add(new XAttribute(nameof(ControllerType), ControllerType));
            xelement.Add(new XAttribute(nameof(ControllerMode), ControllerMode));
            xelement.Add(new XAttribute(nameof(EnableA120Ohm), EnableA120Ohm));
            xelement.Add(new XAttribute(nameof(EnableFIFO), EnableFIFO));
            xelement.Add(new XAttribute(nameof(EnableErrorFrame), EnableErrorFrame));
            xelement.Add(new XAttribute(nameof(EnableTurbe), EnableTurbe));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(Arbitration_phase1), Arbitration_phase1));
            xelement.Add(new XAttribute(nameof(Arbitration_phase2), Arbitration_phase2));
            xelement.Add(new XAttribute(nameof(Arbitration_sjw), Arbitration_sjw));
            xelement.Add(new XAttribute(nameof(Arbitration_prescale), Arbitration_prescale));
            xelement.Add(new XAttribute(nameof(Data_phase1), Data_phase1));
            xelement.Add(new XAttribute(nameof(Data_phase2), Data_phase2));
            xelement.Add(new XAttribute(nameof(Data_sjw), Data_sjw));
            xelement.Add(new XAttribute(nameof(Data_prescale), Data_prescale));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            if (FilterIDArray?.Count > 0)
            {
                XElement filters = new XElement(nameof(FilterIDArray));
                for (int i = 0; i < FilterIDArray.Count; i++)
                {
                    filters.Add(new XAttribute("ID_" + FilterIDArray[i].ToString(), FilterIDArray[i]));
                }
                xelement.Add(filters);
            }
            return xelement;
        }
    }
}
