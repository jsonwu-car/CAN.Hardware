using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class NiCANConfig : BusConfigBase, IXmlSerializer
    {
        private string _interfaceName = "CAN1";
        private NiCanBitrate _canBitrate = NiCanBitrate.niBAUD_500K;
        private bool _enableCustomBitrate = false;
        // 自定义位时序默认值: 40MHz 时基下 500K、采样点 87.5%(Prescale=5, Tq=16, TSEG1=Prop+Phase1=13, TSEG2=Phase2=2)
        private int _tq = 16;
        private int _phase1 = 5;
        private int _phase2 = 2;
        private int _sjw = 1;
        private int _prop = 8;
        private int _prescale = 5;

        [Description("NI-XNET接口名(如CAN1,可在NI MAX中查看/重命名)")]
        public string InterfaceName
        {
            get => _interfaceName;
            set => SetProperty(ref _interfaceName, value);
        }

        [Description("CAN波特率")]
        public NiCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }

        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        [Description("总时间量子数(EnableCustomBitrate=true 时生效)")]
        public int Tq
        {
            get => _tq;
            set => SetProperty(ref _tq, value);
        }

        [Description("相位段1(EnableCustomBitrate=true 时生效)")]
        public int Phase1
        {
            get => _phase1;
            set => SetProperty(ref _phase1, value);
        }

        [Description("相位段2(EnableCustomBitrate=true 时生效)")]
        public int Phase2
        {
            get => _phase2;
            set => SetProperty(ref _phase2, value);
        }

        [Description("同步跳转宽度(EnableCustomBitrate=true 时生效)")]
        public int Sjw
        {
            get => _sjw;
            set => SetProperty(ref _sjw, value);
        }

        [Description("传播时间段(EnableCustomBitrate=true 时生效)")]
        public int Prop
        {
            get => _prop;
            set => SetProperty(ref _prop, value);
        }

        [Description("预分频值(EnableCustomBitrate=true 时生效)")]
        public int Prescale
        {
            get => _prescale;
            set => SetProperty(ref _prescale, value);
        }

        public NiCANConfig() { }

        public void Init(XElement xElement)
        {
            InterfaceName       = XmlAttributeParser.Get(xElement, nameof(InterfaceName),       InterfaceName,       throwIfMissing: false);
            CanBitrate          = XmlAttributeParser.Get(xElement, nameof(CanBitrate),          CanBitrate,          throwIfMissing: false);
            EnableCustomBitrate = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate), EnableCustomBitrate, throwIfMissing: false);
            Tq                  = XmlAttributeParser.Get(xElement, nameof(Tq),                  Tq,                  throwIfMissing: false);
            Phase1              = XmlAttributeParser.Get(xElement, nameof(Phase1),              Phase1,              throwIfMissing: false);
            Phase2              = XmlAttributeParser.Get(xElement, nameof(Phase2),              Phase2,              throwIfMissing: false);
            Sjw                 = XmlAttributeParser.Get(xElement, nameof(Sjw),                 Sjw,                 throwIfMissing: false);
            Prop                = XmlAttributeParser.Get(xElement, nameof(Prop),                Prop,                throwIfMissing: false);
            Prescale            = XmlAttributeParser.Get(xElement, nameof(Prescale),            Prescale,            throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(InterfaceName), InterfaceName));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(Tq), Tq));
            xelement.Add(new XAttribute(nameof(Phase1), Phase1));
            xelement.Add(new XAttribute(nameof(Phase2), Phase2));
            xelement.Add(new XAttribute(nameof(Sjw), Sjw));
            xelement.Add(new XAttribute(nameof(Prop), Prop));
            xelement.Add(new XAttribute(nameof(Prescale), Prescale));
            return xelement;
        }
    }
}
