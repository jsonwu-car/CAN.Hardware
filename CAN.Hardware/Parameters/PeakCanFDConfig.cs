using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class PeakCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private ushort _canFdDeviceChannel = 0x51;  // PCAN_USBBUS1
        private uint _clock = 80_000_000;
        private uint _arbitration_Brp = 10;
        private uint _arbitration_Tseg1 = 12;
        private uint _arbitration_Tseg2 = 3;
        private uint _arbitration_Sjw = 1;
        private uint _data_Brp = 4;
        private uint _data_Tseg1 = 7;
        private uint _data_Tseg2 = 2;
        private uint _data_Sjw = 1;
        private bool _enableFilter = false;
        private uint _filter_FromId = 0;
        private uint _filter_ToId = 0;
        [Description("CAN FD设备通道句柄(如0x51=PCAN_USBBUS1)")]
        public ushort CanFdDeviceChannel
        {
            get => _canFdDeviceChannel;
            set => SetProperty(ref _canFdDeviceChannel, value);
        }
        [Description("控制器时钟频率(Hz)")]
        public uint Clock
        {
            get => _clock;
            set => SetProperty(ref _clock, value);
        }
        [Description("仲裁段波特率预分频")]
        public uint Arbitration_Brp
        {
            get => _arbitration_Brp;
            set => SetProperty(ref _arbitration_Brp, value);
        }
        [Description("仲裁段时间段1")]
        public uint Arbitration_Tseg1
        {
            get => _arbitration_Tseg1;
            set => SetProperty(ref _arbitration_Tseg1, value);
        }
        [Description("仲裁段时间段2")]
        public uint Arbitration_Tseg2
        {
            get => _arbitration_Tseg2;
            set => SetProperty(ref _arbitration_Tseg2, value);
        }
        [Description("仲裁段同步跳转宽度")]
        public uint Arbitration_Sjw
        {
            get => _arbitration_Sjw;
            set => SetProperty(ref _arbitration_Sjw, value);
        }
        [Description("数据段波特率预分频")]
        public uint Data_Brp
        {
            get => _data_Brp;
            set => SetProperty(ref _data_Brp, value);
        }
        [Description("数据段时间段1")]
        public uint Data_Tseg1
        {
            get => _data_Tseg1;
            set => SetProperty(ref _data_Tseg1, value);
        }
        [Description("数据段时间段2")]
        public uint Data_Tseg2
        {
            get => _data_Tseg2;
            set => SetProperty(ref _data_Tseg2, value);
        }
        [Description("数据段同步跳转宽度")]
        public uint Data_Sjw
        {
            get => _data_Sjw;
            set => SetProperty(ref _data_Sjw, value);
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
        public PeakCanFDConfig() { }
        public void Init(XElement xElement)
        {
            CanFdDeviceChannel = XmlAttributeParser.Get(xElement, nameof(CanFdDeviceChannel), CanFdDeviceChannel, throwIfMissing: false);
            Clock              = XmlAttributeParser.Get(xElement, nameof(Clock),              Clock,              throwIfMissing: false);
            Arbitration_Brp    = XmlAttributeParser.Get(xElement, nameof(Arbitration_Brp),    Arbitration_Brp,    throwIfMissing: false);
            Arbitration_Tseg1  = XmlAttributeParser.Get(xElement, nameof(Arbitration_Tseg1),  Arbitration_Tseg1,  throwIfMissing: false);
            Arbitration_Tseg2  = XmlAttributeParser.Get(xElement, nameof(Arbitration_Tseg2),  Arbitration_Tseg2,  throwIfMissing: false);
            Arbitration_Sjw    = XmlAttributeParser.Get(xElement, nameof(Arbitration_Sjw),    Arbitration_Sjw,    throwIfMissing: false);
            Data_Brp           = XmlAttributeParser.Get(xElement, nameof(Data_Brp),           Data_Brp,           throwIfMissing: false);
            Data_Tseg1         = XmlAttributeParser.Get(xElement, nameof(Data_Tseg1),         Data_Tseg1,         throwIfMissing: false);
            Data_Tseg2         = XmlAttributeParser.Get(xElement, nameof(Data_Tseg2),         Data_Tseg2,         throwIfMissing: false);
            Data_Sjw           = XmlAttributeParser.Get(xElement, nameof(Data_Sjw),           Data_Sjw,           throwIfMissing: false);
            EnableFilter       = XmlAttributeParser.Get(xElement, nameof(EnableFilter),       EnableFilter,       throwIfMissing: false);
            Filter_FromId      = XmlAttributeParser.Get(xElement, nameof(Filter_FromId),      Filter_FromId,      throwIfMissing: false);
            Filter_ToId        = XmlAttributeParser.Get(xElement, nameof(Filter_ToId),        Filter_ToId,        throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(CanFdDeviceChannel), CanFdDeviceChannel));
            xelement.Add(new XAttribute(nameof(Clock), Clock));
            xelement.Add(new XAttribute(nameof(Arbitration_Brp), Arbitration_Brp));
            xelement.Add(new XAttribute(nameof(Arbitration_Tseg1), Arbitration_Tseg1));
            xelement.Add(new XAttribute(nameof(Arbitration_Tseg2), Arbitration_Tseg2));
            xelement.Add(new XAttribute(nameof(Arbitration_Sjw), Arbitration_Sjw));
            xelement.Add(new XAttribute(nameof(Data_Brp), Data_Brp));
            xelement.Add(new XAttribute(nameof(Data_Tseg1), Data_Tseg1));
            xelement.Add(new XAttribute(nameof(Data_Tseg2), Data_Tseg2));
            xelement.Add(new XAttribute(nameof(Data_Sjw), Data_Sjw));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(Filter_FromId), Filter_FromId));
            xelement.Add(new XAttribute(nameof(Filter_ToId), Filter_ToId));
            return xelement;
        }
    }
}
