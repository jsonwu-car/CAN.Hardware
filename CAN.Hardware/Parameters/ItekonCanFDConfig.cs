using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 爱泰科技(Itekon) CANFD 配置。
    /// 对应 iTekCANFD.dll(iTek_ API),适用于 USBCANFD/NetCANFD/WifiCANFD 等 CANFD 系列。
    /// 通道按 <see cref="Protocol"/> 初始化,初始化后仍可收发经典 CAN 帧。
    /// </summary>
    public class ItekonCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private ItekonCanFdDeviceModel _deviceModel = ItekonCanFdDeviceModel.USBCANFD_X200;
        private ushort _deviceIndex = 0;
        private byte _deviceChannel = 0;
        private ItekonCanFdProtocol _protocol = ItekonCanFdProtocol.CANFD;
        private ItekonCanFdStandard _canfdStandard = ItekonCanFdStandard.ISO;
        private ItekonCanFdArbitrationBitrate _arbitrationBitrate = ItekonCanFdArbitrationBitrate._500K;
        private ItekonCanFdDataBitrate _dataBitrate = ItekonCanFdDataBitrate._2M;
        private bool _enableCustomBitrate = false;
        private uint _customArbitrationTiming = 0x00070A02;
        private uint _customDataTiming = 0x00010930;
        private ItekonCanFdMode _mode = ItekonCanFdMode.Normal;
        private ItekonCanFdSendType _sendType = ItekonCanFdSendType.Normal;
        private bool _enableNetConfig = false;
        private ItekonCanFdNetMode _netMode = ItekonCanFdNetMode.TcpClient;
        private string _localIp = "192.168.0.100";
        private ushort _localPort = 4001;
        private string _destIp = "192.168.0.10";
        private ushort _destPort = 4000;
        private bool _enableFilter = false;
        private bool _extendedFilter = false;
        private uint _filter_FromId = 0;
        private uint _filter_ToId = 0;

        [Description("爱泰 CANFD 设备型号")]
        public ItekonCanFdDeviceModel DeviceModel
        {
            get => _deviceModel;
            set => SetProperty(ref _deviceModel, value);
        }

        [Description("设备索引号")]
        public ushort DeviceIndex
        {
            get => _deviceIndex;
            set => SetProperty(ref _deviceIndex, value);
        }

        [Description("设备通道号(0基)")]
        public byte DeviceChannel
        {
            get => _deviceChannel;
            set => SetProperty(ref _deviceChannel, value);
        }

        [Description("通道协议类型(CAN/CANFD/CANFD加速)")]
        public ItekonCanFdProtocol Protocol
        {
            get => _protocol;
            set => SetProperty(ref _protocol, value);
        }

        [Description("CAN FD标准类型(ISO或非ISO)")]
        public ItekonCanFdStandard CANFDStandard
        {
            get => _canfdStandard;
            set => SetProperty(ref _canfdStandard, value);
        }

        [Description("仲裁段波特率")]
        public ItekonCanFdArbitrationBitrate ArbitrationBitrate
        {
            get => _arbitrationBitrate;
            set => SetProperty(ref _arbitrationBitrate, value);
        }

        [Description("数据段波特率")]
        public ItekonCanFdDataBitrate DataBitrate
        {
            get => _dataBitrate;
            set => SetProperty(ref _dataBitrate, value);
        }

        [Description("启用自定义波特率寄存器值")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        [Description("自定义仲裁段波特率寄存器值")]
        public uint CustomArbitrationTiming
        {
            get => _customArbitrationTiming;
            set => SetProperty(ref _customArbitrationTiming, value);
        }

        [Description("自定义数据段波特率寄存器值")]
        public uint CustomDataTiming
        {
            get => _customDataTiming;
            set => SetProperty(ref _customDataTiming, value);
        }

        [Description("工作模式(正常/只听)")]
        public ItekonCanFdMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        [Description("发送方式(正常/自发自收)")]
        public ItekonCanFdSendType SendType
        {
            get => _sendType;
            set => SetProperty(ref _sendType, value);
        }

        [Description("启用网络设备IP配置(仅NETCANFD/WifiCANFD等网络型号有效)")]
        public bool EnableNetConfig
        {
            get => _enableNetConfig;
            set => SetProperty(ref _enableNetConfig, value);
        }

        [Description("网络工作模式(UDP/TCP Server/TCP Client)")]
        public ItekonCanFdNetMode NetMode
        {
            get => _netMode;
            set => SetProperty(ref _netMode, value);
        }

        [Description("本机IP(UDP/TCP Server 模式有效)")]
        public string LocalIp
        {
            get => _localIp;
            set => SetProperty(ref _localIp, value);
        }

        [Description("本机端口(UDP/TCP Server 模式有效)")]
        public ushort LocalPort
        {
            get => _localPort;
            set => SetProperty(ref _localPort, value);
        }

        [Description("目标设备IP(UDP/TCP Client 模式有效)")]
        public string DestIp
        {
            get => _destIp;
            set => SetProperty(ref _destIp, value);
        }

        [Description("目标设备端口(UDP/TCP Client 模式有效)")]
        public ushort DestPort
        {
            get => _destPort;
            set => SetProperty(ref _destPort, value);
        }

        [Description("启用硬件过滤器")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }

        [Description("过滤器使用扩展帧")]
        public bool ExtendedFilter
        {
            get => _extendedFilter;
            set => SetProperty(ref _extendedFilter, value);
        }

        [Description("过滤器起始ID")]
        public uint Filter_FromId
        {
            get => _filter_FromId;
            set => SetProperty(ref _filter_FromId, value);
        }

        [Description("过滤器结束ID")]
        public uint Filter_ToId
        {
            get => _filter_ToId;
            set => SetProperty(ref _filter_ToId, value);
        }

        public ItekonCanFDConfig() { }

        public void Init(XElement xElement)
        {
            DeviceModel              = XmlAttributeParser.Get(xElement, nameof(DeviceModel),              DeviceModel,              throwIfMissing: false);
            DeviceIndex              = XmlAttributeParser.Get(xElement, nameof(DeviceIndex),              DeviceIndex,              throwIfMissing: false);
            DeviceChannel            = XmlAttributeParser.Get(xElement, nameof(DeviceChannel),            DeviceChannel,            throwIfMissing: false);
            Protocol                 = XmlAttributeParser.Get(xElement, nameof(Protocol),                 Protocol,                 throwIfMissing: false);
            CANFDStandard            = XmlAttributeParser.Get(xElement, nameof(CANFDStandard),            CANFDStandard,            throwIfMissing: false);
            ArbitrationBitrate       = XmlAttributeParser.Get(xElement, nameof(ArbitrationBitrate),       ArbitrationBitrate,       throwIfMissing: false);
            DataBitrate              = XmlAttributeParser.Get(xElement, nameof(DataBitrate),              DataBitrate,              throwIfMissing: false);
            EnableCustomBitrate      = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),      EnableCustomBitrate,      throwIfMissing: false);
            CustomArbitrationTiming  = XmlAttributeParser.Get(xElement, nameof(CustomArbitrationTiming),  CustomArbitrationTiming,  throwIfMissing: false);
            CustomDataTiming         = XmlAttributeParser.Get(xElement, nameof(CustomDataTiming),         CustomDataTiming,         throwIfMissing: false);
            Mode                     = XmlAttributeParser.Get(xElement, nameof(Mode),                     Mode,                     throwIfMissing: false);
            SendType                 = XmlAttributeParser.Get(xElement, nameof(SendType),                 SendType,                 throwIfMissing: false);
            EnableNetConfig          = XmlAttributeParser.Get(xElement, nameof(EnableNetConfig),          EnableNetConfig,          throwIfMissing: false);
            NetMode                  = XmlAttributeParser.Get(xElement, nameof(NetMode),                  NetMode,                  throwIfMissing: false);
            LocalIp                  = XmlAttributeParser.Get(xElement, nameof(LocalIp),                  LocalIp,                  throwIfMissing: false);
            LocalPort                = XmlAttributeParser.Get(xElement, nameof(LocalPort),                LocalPort,                throwIfMissing: false);
            DestIp                   = XmlAttributeParser.Get(xElement, nameof(DestIp),                   DestIp,                   throwIfMissing: false);
            DestPort                 = XmlAttributeParser.Get(xElement, nameof(DestPort),                 DestPort,                 throwIfMissing: false);
            EnableFilter             = XmlAttributeParser.Get(xElement, nameof(EnableFilter),             EnableFilter,             throwIfMissing: false);
            ExtendedFilter           = XmlAttributeParser.Get(xElement, nameof(ExtendedFilter),           ExtendedFilter,           throwIfMissing: false);
            Filter_FromId            = XmlAttributeParser.Get(xElement, nameof(Filter_FromId),            Filter_FromId,            throwIfMissing: false);
            Filter_ToId              = XmlAttributeParser.Get(xElement, nameof(Filter_ToId),              Filter_ToId,              throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(DeviceModel), DeviceModel));
            xelement.Add(new XAttribute(nameof(DeviceIndex), DeviceIndex));
            xelement.Add(new XAttribute(nameof(DeviceChannel), DeviceChannel));
            xelement.Add(new XAttribute(nameof(Protocol), Protocol));
            xelement.Add(new XAttribute(nameof(CANFDStandard), CANFDStandard));
            xelement.Add(new XAttribute(nameof(ArbitrationBitrate), ArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(DataBitrate), DataBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(CustomArbitrationTiming), CustomArbitrationTiming));
            xelement.Add(new XAttribute(nameof(CustomDataTiming), CustomDataTiming));
            xelement.Add(new XAttribute(nameof(Mode), Mode));
            xelement.Add(new XAttribute(nameof(SendType), SendType));
            xelement.Add(new XAttribute(nameof(EnableNetConfig), EnableNetConfig));
            xelement.Add(new XAttribute(nameof(NetMode), NetMode));
            xelement.Add(new XAttribute(nameof(LocalIp), LocalIp));
            xelement.Add(new XAttribute(nameof(LocalPort), LocalPort));
            xelement.Add(new XAttribute(nameof(DestIp), DestIp));
            xelement.Add(new XAttribute(nameof(DestPort), DestPort));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(ExtendedFilter), ExtendedFilter));
            xelement.Add(new XAttribute(nameof(Filter_FromId), Filter_FromId));
            xelement.Add(new XAttribute(nameof(Filter_ToId), Filter_ToId));
            return xelement;
        }
    }
}
