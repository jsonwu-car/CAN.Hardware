using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class KvaserCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private uint _canFdDeviceChannel = 0;
        private KvaserCanBitrate _canFdArbitrationBitrate = KvaserCanBitrate.canBITRATE_500K;
        private KvaserCanFdBitrate _canFdDataBitrate = KvaserCanFdBitrate.canFD_BITRATE_2M_80P;
        private int _arbitration_Tq = 40;
        private int _arbitration_phase1 = 8;
        private int _arbitration_phase2 = 8;
        private int _arbitration_sjw = 8;
        private int _arbitration_prop = 23;
        private int _arbitration_prescale = 4;
        private int _data_Tq = 20;
        private int _data_phase1 = 8;
        private int _data_phase2 = 4;
        private int _data_sjw = 4;
        private int _data_prop = 7;
        private int _data_prescale = 2;
        private bool _enableCustomBitrate = false;
        private bool _enableFilter = false;
        private uint _filter_Code = 0;
        private uint _filter_Mask = 0;
        private bool _fileter_IsExtended = false;
        [Description("CAN FD设备通道号")]
        public uint CanFdDeviceChannel
        {
            get => _canFdDeviceChannel;
            set => SetProperty(ref _canFdDeviceChannel, value);
        }
        [Description("仲裁段波特率")]
        public KvaserCanBitrate CanFdArbitrationBitrate
        {
            get => _canFdArbitrationBitrate;
            set => SetProperty(ref _canFdArbitrationBitrate, value);
        }
        [Description("数据段波特率")]
        public KvaserCanFdBitrate CanFdDataBitrate
        {
            get => _canFdDataBitrate;
            set => SetProperty(ref _canFdDataBitrate, value);
        }
        [Description("仲裁段总时间量子数")]
        public int Arbitration_Tq
        {
            get => _arbitration_Tq;
            set => SetProperty(ref _arbitration_Tq, value);
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
        [Description("仲裁段传播时间段")]
        public int Arbitration_prop
        {
            get => _arbitration_prop;
            set => SetProperty(ref _arbitration_prop, value);
        }
        [Description("仲裁段预分频值")]
        public int Arbitration_prescale
        {
            get => _arbitration_prescale;
            set => SetProperty(ref _arbitration_prescale, value);
        }
        [Description("数据段总时间量子数")]
        public int Data_Tq
        {
            get => _data_Tq;
            set => SetProperty(ref _data_Tq, value);
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
        [Description("数据段传播时间段")]
        public int Data_prop
        {
            get => _data_prop;
            set => SetProperty(ref _data_prop, value);
        }
        [Description("数据段预分频值")]
        public int Data_prescale
        {
            get => _data_prescale;
            set => SetProperty(ref _data_prescale, value);
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
        public KvaserCanFDConfig() { }
        public void Init(XElement xElement)
        {
            CanFdDeviceChannel      = XmlAttributeParser.Get(xElement, nameof(CanFdDeviceChannel),      CanFdDeviceChannel,      throwIfMissing: false);
            CanFdArbitrationBitrate = XmlAttributeParser.Get(xElement, nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate, throwIfMissing: false);
            CanFdDataBitrate        = XmlAttributeParser.Get(xElement, nameof(CanFdDataBitrate),        CanFdDataBitrate,        throwIfMissing: false);
            Arbitration_Tq          = XmlAttributeParser.Get(xElement, nameof(Arbitration_Tq),          Arbitration_Tq,          throwIfMissing: false);
            Arbitration_phase1      = XmlAttributeParser.Get(xElement, nameof(Arbitration_phase1),      Arbitration_phase1,      throwIfMissing: false);
            Arbitration_phase2      = XmlAttributeParser.Get(xElement, nameof(Arbitration_phase2),      Arbitration_phase2,      throwIfMissing: false);
            Arbitration_sjw         = XmlAttributeParser.Get(xElement, nameof(Arbitration_sjw),         Arbitration_sjw,         throwIfMissing: false);
            Arbitration_prop        = XmlAttributeParser.Get(xElement, nameof(Arbitration_prop),        Arbitration_prop,        throwIfMissing: false);
            Arbitration_prescale    = XmlAttributeParser.Get(xElement, nameof(Arbitration_prescale),    Arbitration_prescale,    throwIfMissing: false);
            Data_Tq                 = XmlAttributeParser.Get(xElement, nameof(Data_Tq),                 Data_Tq,                 throwIfMissing: false);
            Data_phase1             = XmlAttributeParser.Get(xElement, nameof(Data_phase1),             Data_phase1,             throwIfMissing: false);
            Data_phase2             = XmlAttributeParser.Get(xElement, nameof(Data_phase2),             Data_phase2,             throwIfMissing: false);
            Data_sjw                = XmlAttributeParser.Get(xElement, nameof(Data_sjw),                Data_sjw,                throwIfMissing: false);
            Data_prop               = XmlAttributeParser.Get(xElement, nameof(Data_prop),               Data_prop,               throwIfMissing: false);
            Data_prescale           = XmlAttributeParser.Get(xElement, nameof(Data_prescale),           Data_prescale,           throwIfMissing: false);
            EnableCustomBitrate     = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),     EnableCustomBitrate,     throwIfMissing: false);
            EnableFilter            = XmlAttributeParser.Get(xElement, nameof(EnableFilter),            EnableFilter,            throwIfMissing: false);
            Filter_Code             = XmlAttributeParser.Get(xElement, nameof(Filter_Code),             Filter_Code,             throwIfMissing: false);
            Filter_Mask             = XmlAttributeParser.Get(xElement, nameof(Filter_Mask),             Filter_Mask,             throwIfMissing: false);
            Fileter_IsExtended      = XmlAttributeParser.Get(xElement, nameof(Fileter_IsExtended),      Fileter_IsExtended,      throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(CanFdDeviceChannel), CanFdDeviceChannel));
            xelement.Add(new XAttribute(nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CanFdDataBitrate), CanFdDataBitrate));
            xelement.Add(new XAttribute(nameof(Arbitration_Tq), Arbitration_Tq));
            xelement.Add(new XAttribute(nameof(Arbitration_phase1), Arbitration_phase1));
            xelement.Add(new XAttribute(nameof(Arbitration_phase2), Arbitration_phase2));
            xelement.Add(new XAttribute(nameof(Arbitration_sjw), Arbitration_sjw));
            xelement.Add(new XAttribute(nameof(Arbitration_prop), Arbitration_prop));
            xelement.Add(new XAttribute(nameof(Arbitration_prescale), Arbitration_prescale));
            xelement.Add(new XAttribute(nameof(Data_Tq), Data_Tq));
            xelement.Add(new XAttribute(nameof(Data_phase1), Data_phase1));
            xelement.Add(new XAttribute(nameof(Data_phase2), Data_phase2));
            xelement.Add(new XAttribute(nameof(Data_sjw), Data_sjw));
            xelement.Add(new XAttribute(nameof(Data_prop), Data_prop));
            xelement.Add(new XAttribute(nameof(Data_prescale), Data_prescale));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(Filter_Code), Filter_Code));
            xelement.Add(new XAttribute(nameof(Filter_Mask), Filter_Mask));
            xelement.Add(new XAttribute(nameof(Fileter_IsExtended), Fileter_IsExtended));
            return xelement;
        }
    }
}
