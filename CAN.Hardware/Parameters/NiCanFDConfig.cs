using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class NiCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private string _interfaceName = "CAN1";
        private NiCanBitrate _canFdArbitrationBitrate = NiCanBitrate.niBAUD_500K;
        private NiCanFdBitrate _canFdDataBitrate = NiCanFdBitrate.niFDBAUD_2M;
        private bool _enableCustomBitrate = false;
        // 自定义位时序默认值(40MHz 时基): 仲裁段 500K、采样点 87.5%; 数据段 2M、采样点 80%
        private int _arbitration_Tq = 16;
        private int _arbitration_phase1 = 5;
        private int _arbitration_phase2 = 2;
        private int _arbitration_sjw = 1;
        private int _arbitration_prop = 8;
        private int _arbitration_prescale = 5;
        private int _data_Tq = 20;
        private int _data_phase1 = 5;
        private int _data_phase2 = 4;
        private int _data_sjw = 4;
        private int _data_prop = 10;
        private int _data_prescale = 1;

        [Description("NI-XNET接口名(如CAN1,可在NI MAX中查看/重命名)")]
        public string InterfaceName
        {
            get => _interfaceName;
            set => SetProperty(ref _interfaceName, value);
        }

        [Description("CANFD仲裁段波特率")]
        public NiCanBitrate CanFdArbitrationBitrate
        {
            get => _canFdArbitrationBitrate;
            set => SetProperty(ref _canFdArbitrationBitrate, value);
        }

        [Description("CANFD数据段波特率")]
        public NiCanFdBitrate CanFdDataBitrate
        {
            get => _canFdDataBitrate;
            set => SetProperty(ref _canFdDataBitrate, value);
        }

        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        [Description("仲裁段总时间量子数(EnableCustomBitrate=true 时生效)")]
        public int Arbitration_Tq
        {
            get => _arbitration_Tq;
            set => SetProperty(ref _arbitration_Tq, value);
        }

        [Description("仲裁段相位段1(EnableCustomBitrate=true 时生效)")]
        public int Arbitration_phase1
        {
            get => _arbitration_phase1;
            set => SetProperty(ref _arbitration_phase1, value);
        }

        [Description("仲裁段相位段2(EnableCustomBitrate=true 时生效)")]
        public int Arbitration_phase2
        {
            get => _arbitration_phase2;
            set => SetProperty(ref _arbitration_phase2, value);
        }

        [Description("仲裁段同步跳转宽度(EnableCustomBitrate=true 时生效)")]
        public int Arbitration_sjw
        {
            get => _arbitration_sjw;
            set => SetProperty(ref _arbitration_sjw, value);
        }

        [Description("仲裁段传播时间段(EnableCustomBitrate=true 时生效)")]
        public int Arbitration_prop
        {
            get => _arbitration_prop;
            set => SetProperty(ref _arbitration_prop, value);
        }

        [Description("仲裁段预分频值(EnableCustomBitrate=true 时生效)")]
        public int Arbitration_prescale
        {
            get => _arbitration_prescale;
            set => SetProperty(ref _arbitration_prescale, value);
        }

        [Description("数据段总时间量子数(EnableCustomBitrate=true 时生效)")]
        public int Data_Tq
        {
            get => _data_Tq;
            set => SetProperty(ref _data_Tq, value);
        }

        [Description("数据段相位段1(EnableCustomBitrate=true 时生效)")]
        public int Data_phase1
        {
            get => _data_phase1;
            set => SetProperty(ref _data_phase1, value);
        }

        [Description("数据段相位段2(EnableCustomBitrate=true 时生效)")]
        public int Data_phase2
        {
            get => _data_phase2;
            set => SetProperty(ref _data_phase2, value);
        }

        [Description("数据段同步跳转宽度(EnableCustomBitrate=true 时生效)")]
        public int Data_sjw
        {
            get => _data_sjw;
            set => SetProperty(ref _data_sjw, value);
        }

        [Description("数据段传播时间段(EnableCustomBitrate=true 时生效)")]
        public int Data_prop
        {
            get => _data_prop;
            set => SetProperty(ref _data_prop, value);
        }

        [Description("数据段预分频值(EnableCustomBitrate=true 时生效)")]
        public int Data_prescale
        {
            get => _data_prescale;
            set => SetProperty(ref _data_prescale, value);
        }

        public NiCanFDConfig() { }

        public void Init(XElement xElement)
        {
            InterfaceName           = XmlAttributeParser.Get(xElement, nameof(InterfaceName),           InterfaceName,           throwIfMissing: false);
            CanFdArbitrationBitrate = XmlAttributeParser.Get(xElement, nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate, throwIfMissing: false);
            CanFdDataBitrate        = XmlAttributeParser.Get(xElement, nameof(CanFdDataBitrate),        CanFdDataBitrate,        throwIfMissing: false);
            EnableCustomBitrate     = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),     EnableCustomBitrate,     throwIfMissing: false);
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
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(InterfaceName), InterfaceName));
            xelement.Add(new XAttribute(nameof(CanFdArbitrationBitrate), CanFdArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(CanFdDataBitrate), CanFdDataBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
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
            return xelement;
        }
    }
}
