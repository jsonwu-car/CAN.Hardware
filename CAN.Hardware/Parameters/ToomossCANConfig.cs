using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 图莫斯 USB2XXX 经典 CAN 配置。
    /// </summary>
    public class ToomossCANConfig : BusConfigBase, IXmlSerializer
    {
        private uint _deviceIndex = 0;
        private byte _channelIndex = 0;
        private uint _canBitrate = 500000;
        private ToomossCanMode _mode = ToomossCanMode.Normal;
        private bool _enableA120Ohm = false;
        private bool _enableAutoOffline = false;
        private bool _disableRetransmission = false;
        private bool _fifoLock = false;
        private bool _txPriorityByRequest = false;
        private bool _enableFilter = false;
        private byte _filterIndex = 0;
        private ToomossCanFilterMode _filterMode = ToomossCanFilterMode.Mask;
        private bool _extendedFilter = false;
        private uint _filterId = 0;
        private uint _filterRtr = 0;
        private uint _filterMask = 0xFFFFFFFF;
        private uint _filterMaskIde = 0;
        private uint _filterMaskRtr = 0;

        [Description("设备索引号(USB_ScanDevice 返回的设备序号)")]
        public uint DeviceIndex
        {
            get => _deviceIndex;
            set => SetProperty(ref _deviceIndex, value);
        }

        [Description("CAN通道号(0基)")]
        public byte ChannelIndex
        {
            get => _channelIndex;
            set => SetProperty(ref _channelIndex, value);
        }

        [Description("CAN波特率(bps,由 CAN_GetCANSpeedArg 自动计算时序)")]
        public uint CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }

        [Description("CAN工作模式(正常/环回/静默/静默环回)")]
        public ToomossCanMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        [Description("启用内部120欧姆终端电阻")]
        public bool EnableA120Ohm
        {
            get => _enableA120Ohm;
            set => SetProperty(ref _enableA120Ohm, value);
        }

        [Description("启用自动离线管理(CAN_ABOM)")]
        public bool EnableAutoOffline
        {
            get => _enableAutoOffline;
            set => SetProperty(ref _enableAutoOffline, value);
        }

        [Description("禁止报文自动重传(CAN_NART=1)")]
        public bool DisableRetransmission
        {
            get => _disableRetransmission;
            set => SetProperty(ref _disableRetransmission, value);
        }

        [Description("FIFO锁定:false=新报文覆盖旧报文,true=丢弃新报文")]
        public bool FifoLock
        {
            get => _fifoLock;
            set => SetProperty(ref _fifoLock, value);
        }

        [Description("发送优先级:false=ID决定,true=请求顺序决定")]
        public bool TxPriorityByRequest
        {
            get => _txPriorityByRequest;
            set => SetProperty(ref _txPriorityByRequest, value);
        }

        [Description("启用经典 CAN 硬件过滤器(不启用时配置为接收所有帧)")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }

        [Description("过滤器索引号(0~13)")]
        public byte FilterIndex
        {
            get => _filterIndex;
            set => SetProperty(ref _filterIndex, value);
        }

        [Description("过滤器模式(屏蔽位/标识符列表)")]
        public ToomossCanFilterMode FilterMode
        {
            get => _filterMode;
            set => SetProperty(ref _filterMode, value);
        }

        [Description("过滤器使用扩展帧")]
        public bool ExtendedFilter
        {
            get => _extendedFilter;
            set => SetProperty(ref _extendedFilter, value);
        }

        [Description("验收码ID(ID_Std_Ext)")]
        public uint FilterId
        {
            get => _filterId;
            set => SetProperty(ref _filterId, value);
        }

        [Description("验收码RTR(ID_RTR):数据帧=1,远程帧=0")]
        public uint FilterRtr
        {
            get => _filterRtr;
            set => SetProperty(ref _filterRtr, value);
        }

        [Description("屏蔽码ID(MASK_Std_Ext),对应位为1则需要比较")]
        public uint FilterMask
        {
            get => _filterMask;
            set => SetProperty(ref _filterMask, value);
        }

        [Description("屏蔽码IDE(MASK_IDE):1=参与比较")]
        public uint FilterMaskIde
        {
            get => _filterMaskIde;
            set => SetProperty(ref _filterMaskIde, value);
        }

        [Description("屏蔽码RTR(MASK_RTR):1=参与比较")]
        public uint FilterMaskRtr
        {
            get => _filterMaskRtr;
            set => SetProperty(ref _filterMaskRtr, value);
        }

        public ToomossCANConfig() { }

        public void Init(XElement xElement)
        {
            DeviceIndex          = XmlAttributeParser.Get(xElement, nameof(DeviceIndex),          DeviceIndex,          throwIfMissing: false);
            ChannelIndex         = XmlAttributeParser.Get(xElement, nameof(ChannelIndex),         ChannelIndex,         throwIfMissing: false);
            CanBitrate           = XmlAttributeParser.Get(xElement, nameof(CanBitrate),           CanBitrate,           throwIfMissing: false);
            Mode                 = XmlAttributeParser.Get(xElement, nameof(Mode),                 Mode,                 throwIfMissing: false);
            EnableA120Ohm        = XmlAttributeParser.Get(xElement, nameof(EnableA120Ohm),        EnableA120Ohm,        throwIfMissing: false);
            EnableAutoOffline    = XmlAttributeParser.Get(xElement, nameof(EnableAutoOffline),    EnableAutoOffline,    throwIfMissing: false);
            DisableRetransmission = XmlAttributeParser.Get(xElement, nameof(DisableRetransmission), DisableRetransmission, throwIfMissing: false);
            FifoLock             = XmlAttributeParser.Get(xElement, nameof(FifoLock),             FifoLock,             throwIfMissing: false);
            TxPriorityByRequest  = XmlAttributeParser.Get(xElement, nameof(TxPriorityByRequest),  TxPriorityByRequest,  throwIfMissing: false);
            EnableFilter         = XmlAttributeParser.Get(xElement, nameof(EnableFilter),         EnableFilter,         throwIfMissing: false);
            FilterIndex          = XmlAttributeParser.Get(xElement, nameof(FilterIndex),          FilterIndex,          throwIfMissing: false);
            FilterMode           = XmlAttributeParser.Get(xElement, nameof(FilterMode),           FilterMode,           throwIfMissing: false);
            ExtendedFilter       = XmlAttributeParser.Get(xElement, nameof(ExtendedFilter),       ExtendedFilter,       throwIfMissing: false);
            FilterId             = XmlAttributeParser.Get(xElement, nameof(FilterId),             FilterId,             throwIfMissing: false);
            FilterRtr            = XmlAttributeParser.Get(xElement, nameof(FilterRtr),            FilterRtr,            throwIfMissing: false);
            FilterMask           = XmlAttributeParser.Get(xElement, nameof(FilterMask),           FilterMask,           throwIfMissing: false);
            FilterMaskIde        = XmlAttributeParser.Get(xElement, nameof(FilterMaskIde),        FilterMaskIde,        throwIfMissing: false);
            FilterMaskRtr        = XmlAttributeParser.Get(xElement, nameof(FilterMaskRtr),        FilterMaskRtr,        throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(DeviceIndex), DeviceIndex));
            xelement.Add(new XAttribute(nameof(ChannelIndex), ChannelIndex));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(Mode), Mode));
            xelement.Add(new XAttribute(nameof(EnableA120Ohm), EnableA120Ohm));
            xelement.Add(new XAttribute(nameof(EnableAutoOffline), EnableAutoOffline));
            xelement.Add(new XAttribute(nameof(DisableRetransmission), DisableRetransmission));
            xelement.Add(new XAttribute(nameof(FifoLock), FifoLock));
            xelement.Add(new XAttribute(nameof(TxPriorityByRequest), TxPriorityByRequest));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(FilterIndex), FilterIndex));
            xelement.Add(new XAttribute(nameof(FilterMode), FilterMode));
            xelement.Add(new XAttribute(nameof(ExtendedFilter), ExtendedFilter));
            xelement.Add(new XAttribute(nameof(FilterId), FilterId));
            xelement.Add(new XAttribute(nameof(FilterRtr), FilterRtr));
            xelement.Add(new XAttribute(nameof(FilterMask), FilterMask));
            xelement.Add(new XAttribute(nameof(FilterMaskIde), FilterMaskIde));
            xelement.Add(new XAttribute(nameof(FilterMaskRtr), FilterMaskRtr));
            return xelement;
        }
    }
}
