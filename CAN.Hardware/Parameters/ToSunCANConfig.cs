using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.BaseLibDll;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class ToSunCANConfig : BusConfigBase, IXmlSerializer
    {
        private CHANNEL_INDEX _channelIndex = CHANNEL_INDEX.CHN1;
        private double _canBitrate = 500;
        private bool _enableA120Ohm = true;
        private bool _enableFIFO = true;
        private bool _enableErrorFrame = true;
        private bool _enableTurbe = false;
        private bool _enableFilter = false;
        private List<uint>? _filterIDArray = null;
        [Description("CAN通道索引(0基,CHN1=0)")]
        public CHANNEL_INDEX ChannelIndex
        {
            get => _channelIndex;
            set => SetProperty(ref _channelIndex, value);
        }
        [Description("CAN波特率(Kbps)")]
        public double CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
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
        public ToSunCANConfig() { }
        public void Init(XElement xElement)
        {
            ChannelIndex     = XmlAttributeParser.Get(xElement, nameof(ChannelIndex),     ChannelIndex,     throwIfMissing: false);
            CanBitrate       = XmlAttributeParser.Get(xElement, nameof(CanBitrate),       CanBitrate,       throwIfMissing: false);
            EnableA120Ohm    = XmlAttributeParser.Get(xElement, nameof(EnableA120Ohm),    EnableA120Ohm,    throwIfMissing: false);
            EnableFIFO       = XmlAttributeParser.Get(xElement, nameof(EnableFIFO),       EnableFIFO,       throwIfMissing: false);
            EnableErrorFrame = XmlAttributeParser.Get(xElement, nameof(EnableErrorFrame), EnableErrorFrame, throwIfMissing: false);
            EnableTurbe      = XmlAttributeParser.Get(xElement, nameof(EnableTurbe),      EnableTurbe,      throwIfMissing: false);
            EnableFilter     = XmlAttributeParser.Get(xElement, nameof(EnableFilter),     EnableFilter,     throwIfMissing: false);

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
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(EnableA120Ohm), EnableA120Ohm));
            xelement.Add(new XAttribute(nameof(EnableFIFO), EnableFIFO));
            xelement.Add(new XAttribute(nameof(EnableErrorFrame), EnableErrorFrame));
            xelement.Add(new XAttribute(nameof(EnableTurbe), EnableTurbe));
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
