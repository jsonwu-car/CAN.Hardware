using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class PeakCANConfig : BusConfigBase, IXmlSerializer
    {
        private ushort _canDeviceChannel = 0x51;    // PCAN_USBBUS1
        private PeakCanBitrate _canBitrate = PeakCanBitrate.pcanBAUD_500K;
        private bool _enableFilter = false;
        private uint _filter_FromId = 0;
        private uint _filter_ToId = 0;
        private uint _ioPort = 0;
        private ushort _interrupt = 0;
        [Description("CAN设备通道句柄(如0x51=PCAN_USBBUS1)")]
        public ushort CanDeviceChannel
        {
            get => _canDeviceChannel;
            set => SetProperty(ref _canDeviceChannel, value);
        }
        [Description("CAN波特率")]
        public PeakCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }
        [Description("启用硬件过滤器")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }
        // PEAK 过滤器使用 ID 区间(FromID~ToID),而非 Kvaser 的 Code/Mask 掩码方式
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
        [Description("I/O端口地址(仅非即插即用设备使用)")]
        public uint IoPort
        {
            get => _ioPort;
            set => SetProperty(ref _ioPort, value);
        }
        [Description("中断号(仅非即插即用设备使用)")]
        public ushort Interrupt
        {
            get => _interrupt;
            set => SetProperty(ref _interrupt, value);
        }
        public PeakCANConfig() { }
        public void Init(XElement xElement)
        {
            CanDeviceChannel = XmlAttributeParser.Get(xElement, nameof(CanDeviceChannel), CanDeviceChannel, throwIfMissing: false);
            CanBitrate       = XmlAttributeParser.Get(xElement, nameof(CanBitrate),       CanBitrate,       throwIfMissing: false);
            EnableFilter     = XmlAttributeParser.Get(xElement, nameof(EnableFilter),     EnableFilter,     throwIfMissing: false);
            Filter_FromId    = XmlAttributeParser.Get(xElement, nameof(Filter_FromId),    Filter_FromId,    throwIfMissing: false);
            Filter_ToId      = XmlAttributeParser.Get(xElement, nameof(Filter_ToId),      Filter_ToId,      throwIfMissing: false);
            IoPort           = XmlAttributeParser.Get(xElement, nameof(IoPort),           IoPort,           throwIfMissing: false);
            Interrupt        = XmlAttributeParser.Get(xElement, nameof(Interrupt),        Interrupt,        throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(CanDeviceChannel), CanDeviceChannel));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(Filter_FromId), Filter_FromId));
            xelement.Add(new XAttribute(nameof(Filter_ToId), Filter_ToId));
            xelement.Add(new XAttribute(nameof(IoPort), IoPort));
            xelement.Add(new XAttribute(nameof(Interrupt), Interrupt));
            return xelement;
        }
    }
}
