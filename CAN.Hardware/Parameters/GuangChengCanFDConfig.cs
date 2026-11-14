using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 广成科技 USBCANFD 分析仪配置(ECANFDVCI64.dll)。
    /// 通道按 CANFD 初始化,同一个通道既能收发 CANFD 帧也能收发经典 CAN 帧;
    /// <see cref="UseCanFD"/> 用于限制是否允许 FD 帧。
    /// </summary>
    public class GuangChengCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private uint _deviceIndex = 0;
        private uint _deviceChannel = 0;
        private bool _useCanFD = true;
        private GuangChengCanFdReceiveMode _receiveMode = GuangChengCanFdReceiveMode.GlobalStandardAndExtended;
        private GuangChengCanFdSendMode _sendMode = GuangChengCanFdSendMode.Passive;
        private GuangChengCanFdNominalBitrate _nominalBitrate = GuangChengCanFdNominalBitrate._500K;
        private GuangChengCanFdDataBitrate _dataBitrate = GuangChengCanFdDataBitrate._2M;
        private bool _enableFilter = false;
        private bool _filterExtended = false;
        private uint _filterId = 0;
        private uint _filterMask = 0x7FF;

        [Description("设备索引号(按 SN 从小到大分配)")]
        public uint DeviceIndex
        {
            get => _deviceIndex;
            set => SetProperty(ref _deviceIndex, value);
        }

        [Description("设备通道号(0基,对应 CAN1/CAN2)")]
        public uint DeviceChannel
        {
            get => _deviceChannel;
            set => SetProperty(ref _deviceChannel, value);
        }

        [Description("是否收发 CAN FD 帧(false=仅经典 CAN,通道仍按 CANFD 初始化)")]
        public bool UseCanFD
        {
            get => _useCanFD;
            set => SetProperty(ref _useCanFD, value);
        }

        [Description("接收模式(特定/标准帧/扩展帧/标准+扩展)")]
        public GuangChengCanFdReceiveMode ReceiveMode
        {
            get => _receiveMode;
            set => SetProperty(ref _receiveMode, value);
        }

        [Description("发送模式(主动/被动)")]
        public GuangChengCanFdSendMode SendMode
        {
            get => _sendMode;
            set => SetProperty(ref _sendMode, value);
        }

        [Description("仲裁段波特率(Nominal Bit Rate)")]
        public GuangChengCanFdNominalBitrate NominalBitrate
        {
            get => _nominalBitrate;
            set => SetProperty(ref _nominalBitrate, value);
        }

        [Description("数据段波特率(Data Bit Rate)")]
        public GuangChengCanFdDataBitrate DataBitrate
        {
            get => _dataBitrate;
            set => SetProperty(ref _dataBitrate, value);
        }

        [Description("启用硬件滤波器(特定接收模式,使用滤波器1)")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }

        [Description("滤波器使用扩展帧")]
        public bool FilterExtended
        {
            get => _filterExtended;
            set => SetProperty(ref _filterExtended, value);
        }

        [Description("滤波器 ID")]
        public uint FilterId
        {
            get => _filterId;
            set => SetProperty(ref _filterId, value);
        }

        [Description("滤波器屏蔽码(标准帧推荐 0x7FF,扩展帧推荐 0x1FFFFFFF)")]
        public uint FilterMask
        {
            get => _filterMask;
            set => SetProperty(ref _filterMask, value);
        }

        public GuangChengCanFDConfig() { }

        public void Init(XElement xElement)
        {
            DeviceIndex    = XmlAttributeParser.Get(xElement, nameof(DeviceIndex),    DeviceIndex,    throwIfMissing: false);
            DeviceChannel  = XmlAttributeParser.Get(xElement, nameof(DeviceChannel),  DeviceChannel,  throwIfMissing: false);
            UseCanFD       = XmlAttributeParser.Get(xElement, nameof(UseCanFD),       UseCanFD,       throwIfMissing: false);
            ReceiveMode    = XmlAttributeParser.Get(xElement, nameof(ReceiveMode),    ReceiveMode,    throwIfMissing: false);
            SendMode       = XmlAttributeParser.Get(xElement, nameof(SendMode),       SendMode,       throwIfMissing: false);
            NominalBitrate = XmlAttributeParser.Get(xElement, nameof(NominalBitrate), NominalBitrate, throwIfMissing: false);
            DataBitrate    = XmlAttributeParser.Get(xElement, nameof(DataBitrate),    DataBitrate,    throwIfMissing: false);
            EnableFilter   = XmlAttributeParser.Get(xElement, nameof(EnableFilter),   EnableFilter,   throwIfMissing: false);
            FilterExtended = XmlAttributeParser.Get(xElement, nameof(FilterExtended), FilterExtended, throwIfMissing: false);
            FilterId       = XmlAttributeParser.Get(xElement, nameof(FilterId),       FilterId,       throwIfMissing: false);
            FilterMask     = XmlAttributeParser.Get(xElement, nameof(FilterMask),     FilterMask,     throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(DeviceIndex), DeviceIndex));
            xelement.Add(new XAttribute(nameof(DeviceChannel), DeviceChannel));
            xelement.Add(new XAttribute(nameof(UseCanFD), UseCanFD));
            xelement.Add(new XAttribute(nameof(ReceiveMode), ReceiveMode));
            xelement.Add(new XAttribute(nameof(SendMode), SendMode));
            xelement.Add(new XAttribute(nameof(NominalBitrate), NominalBitrate));
            xelement.Add(new XAttribute(nameof(DataBitrate), DataBitrate));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(FilterExtended), FilterExtended));
            xelement.Add(new XAttribute(nameof(FilterId), FilterId));
            xelement.Add(new XAttribute(nameof(FilterMask), FilterMask));
            return xelement;
        }
    }
}
