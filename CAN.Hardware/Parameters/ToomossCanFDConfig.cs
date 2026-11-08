using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 图莫斯 USB2XXX CANFD 配置。
    /// </summary>
    public class ToomossCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private uint _deviceIndex = 0;
        private byte _channelIndex = 0;
        private uint _arbitrationBitrate = 500000;
        private uint _dataBitrate = 2000000;
        private ToomossCanFdMode _mode = ToomossCanFdMode.Normal;
        private bool _enableISOCRC = true;
        private bool _enableRetrySend = true;
        private bool _enableA120Ohm = false;
        private bool _enableFilter = false;
        private byte _filterIndex = 0;
        private bool _extendedFilter = false;
        private uint _filterId = 0;
        private uint _filterMask = 0x1FFFFFFF;

        [Description("设备索引号(USB_ScanDevice 返回的设备序号)")]
        public uint DeviceIndex
        {
            get => _deviceIndex;
            set => SetProperty(ref _deviceIndex, value);
        }

        [Description("CANFD通道号(0基)")]
        public byte ChannelIndex
        {
            get => _channelIndex;
            set => SetProperty(ref _channelIndex, value);
        }

        [Description("仲裁段波特率(bps,由 CANFD_GetCANSpeedArg 自动计算时序)")]
        public uint ArbitrationBitrate
        {
            get => _arbitrationBitrate;
            set => SetProperty(ref _arbitrationBitrate, value);
        }

        [Description("数据段波特率(bps)")]
        public uint DataBitrate
        {
            get => _dataBitrate;
            set => SetProperty(ref _dataBitrate, value);
        }

        [Description("CANFD工作模式(正常/自发自收)")]
        public ToomossCanFdMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        [Description("启用 ISO CRC")]
        public bool EnableISOCRC
        {
            get => _enableISOCRC;
            set => SetProperty(ref _enableISOCRC, value);
        }

        [Description("启用报文自动重传")]
        public bool EnableRetrySend
        {
            get => _enableRetrySend;
            set => SetProperty(ref _enableRetrySend, value);
        }

        [Description("启用内部120欧姆终端电阻")]
        public bool EnableA120Ohm
        {
            get => _enableA120Ohm;
            set => SetProperty(ref _enableA120Ohm, value);
        }

        [Description("启用 CANFD 硬件过滤器(不启用时默认接收所有帧)")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }

        [Description("过滤器索引号(0~31)")]
        public byte FilterIndex
        {
            get => _filterIndex;
            set => SetProperty(ref _filterIndex, value);
        }

        [Description("过滤器使用扩展帧")]
        public bool ExtendedFilter
        {
            get => _extendedFilter;
            set => SetProperty(ref _extendedFilter, value);
        }

        [Description("验收码ID(bit28..0)")]
        public uint FilterId
        {
            get => _filterId;
            set => SetProperty(ref _filterId, value);
        }

        [Description("屏蔽码(bit28..0),对应位为1则需要比较")]
        public uint FilterMask
        {
            get => _filterMask;
            set => SetProperty(ref _filterMask, value);
        }

        public ToomossCanFDConfig() { }

        public void Init(XElement xElement)
        {
            DeviceIndex        = XmlAttributeParser.Get(xElement, nameof(DeviceIndex),        DeviceIndex,        throwIfMissing: false);
            ChannelIndex       = XmlAttributeParser.Get(xElement, nameof(ChannelIndex),       ChannelIndex,       throwIfMissing: false);
            ArbitrationBitrate = XmlAttributeParser.Get(xElement, nameof(ArbitrationBitrate), ArbitrationBitrate, throwIfMissing: false);
            DataBitrate        = XmlAttributeParser.Get(xElement, nameof(DataBitrate),        DataBitrate,        throwIfMissing: false);
            Mode               = XmlAttributeParser.Get(xElement, nameof(Mode),               Mode,               throwIfMissing: false);
            EnableISOCRC       = XmlAttributeParser.Get(xElement, nameof(EnableISOCRC),       EnableISOCRC,       throwIfMissing: false);
            EnableRetrySend    = XmlAttributeParser.Get(xElement, nameof(EnableRetrySend),    EnableRetrySend,    throwIfMissing: false);
            EnableA120Ohm      = XmlAttributeParser.Get(xElement, nameof(EnableA120Ohm),      EnableA120Ohm,      throwIfMissing: false);
            EnableFilter       = XmlAttributeParser.Get(xElement, nameof(EnableFilter),       EnableFilter,       throwIfMissing: false);
            FilterIndex        = XmlAttributeParser.Get(xElement, nameof(FilterIndex),        FilterIndex,        throwIfMissing: false);
            ExtendedFilter     = XmlAttributeParser.Get(xElement, nameof(ExtendedFilter),     ExtendedFilter,     throwIfMissing: false);
            FilterId           = XmlAttributeParser.Get(xElement, nameof(FilterId),           FilterId,           throwIfMissing: false);
            FilterMask         = XmlAttributeParser.Get(xElement, nameof(FilterMask),         FilterMask,         throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(DeviceIndex), DeviceIndex));
            xelement.Add(new XAttribute(nameof(ChannelIndex), ChannelIndex));
            xelement.Add(new XAttribute(nameof(ArbitrationBitrate), ArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(DataBitrate), DataBitrate));
            xelement.Add(new XAttribute(nameof(Mode), Mode));
            xelement.Add(new XAttribute(nameof(EnableISOCRC), EnableISOCRC));
            xelement.Add(new XAttribute(nameof(EnableRetrySend), EnableRetrySend));
            xelement.Add(new XAttribute(nameof(EnableA120Ohm), EnableA120Ohm));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(FilterIndex), FilterIndex));
            xelement.Add(new XAttribute(nameof(ExtendedFilter), ExtendedFilter));
            xelement.Add(new XAttribute(nameof(FilterId), FilterId));
            xelement.Add(new XAttribute(nameof(FilterMask), FilterMask));
            return xelement;
        }
    }
}
