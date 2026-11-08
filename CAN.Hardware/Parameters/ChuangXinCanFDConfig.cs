using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 创芯科技 USBCANFD 分析仪配置。
    /// 适用于 USBCANFD-200U/100U/MINI(ControlCANFD.dll),可切换 CANFD/经典 CAN 模式。
    /// </summary>
    public class ChuangXinCanFDConfig : BusConfigBase, IXmlSerializer
    {
        private ChuangXinCanFdDeviceModel _deviceModel = ChuangXinCanFdDeviceModel.USBCANFD_200U;
        private uint _deviceIndex = 0;
        private uint _deviceChannel = 0;
        private ChuangXinCANFDStandard _canfdStandard = ChuangXinCANFDStandard.CANFD_ISO;
        private bool _useCanFD = true;
        private ChuangXinCanBitrate _arbitrationBitrate = ChuangXinCanBitrate._500K;
        private ChuangXinCanFdBitrate _dataBitrate = ChuangXinCanFdBitrate._2M;
        private bool _enableCustomBitrate = false;
        private string _customBitrateContent = string.Empty;
        private ChuangXinCanFdMode _mode = ChuangXinCanFdMode.Normal;
        private bool _enableA120Ohm = false;
        private bool _enableFilter = false;
        private bool _extendedFilter = false;
        private uint _filter_FromId = 0;
        private uint _filter_ToId = 0;

        [Description("创芯 USBCANFD 设备型号")]
        public ChuangXinCanFdDeviceModel DeviceModel
        {
            get => _deviceModel;
            set => SetProperty(ref _deviceModel, value);
        }

        [Description("设备索引号")]
        public uint DeviceIndex
        {
            get => _deviceIndex;
            set => SetProperty(ref _deviceIndex, value);
        }

        [Description("设备通道号(0基)")]
        public uint DeviceChannel
        {
            get => _deviceChannel;
            set => SetProperty(ref _deviceChannel, value);
        }

        [Description("CAN FD标准类型(ISO或BOSCH非ISO)")]
        public ChuangXinCANFDStandard CANFDStandard
        {
            get => _canfdStandard;
            set => SetProperty(ref _canfdStandard, value);
        }

        [Description("是否收发 CAN FD 帧(false=仅经典 CAN,通道仍按 CANFD 初始化)")]
        public bool UseCanFD
        {
            get => _useCanFD;
            set => SetProperty(ref _useCanFD, value);
        }

        [Description("仲裁段波特率")]
        public ChuangXinCanBitrate ArbitrationBitrate
        {
            get => _arbitrationBitrate;
            set => SetProperty(ref _arbitrationBitrate, value);
        }

        [Description("数据段波特率")]
        public ChuangXinCanFdBitrate DataBitrate
        {
            get => _dataBitrate;
            set => SetProperty(ref _dataBitrate, value);
        }

        [Description("启用自定义波特率")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        [Description("自定义波特率内容(由厂商波特率计算工具生成)")]
        public string CustomBitrateContent
        {
            get => _customBitrateContent;
            set => SetProperty(ref _customBitrateContent, value);
        }

        [Description("工作模式(正常/只听)")]
        public ChuangXinCanFdMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        [Description("启用120欧姆终端电阻")]
        public bool EnableA120Ohm
        {
            get => _enableA120Ohm;
            set => SetProperty(ref _enableA120Ohm, value);
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

        public ChuangXinCanFDConfig() { }

        public void Init(XElement xElement)
        {
            DeviceModel          = XmlAttributeParser.Get(xElement, nameof(DeviceModel),          DeviceModel,          throwIfMissing: false);
            DeviceIndex          = XmlAttributeParser.Get(xElement, nameof(DeviceIndex),          DeviceIndex,          throwIfMissing: false);
            DeviceChannel        = XmlAttributeParser.Get(xElement, nameof(DeviceChannel),        DeviceChannel,        throwIfMissing: false);
            CANFDStandard        = XmlAttributeParser.Get(xElement, nameof(CANFDStandard),        CANFDStandard,        throwIfMissing: false);
            UseCanFD             = XmlAttributeParser.Get(xElement, nameof(UseCanFD),             UseCanFD,             throwIfMissing: false);
            ArbitrationBitrate   = XmlAttributeParser.Get(xElement, nameof(ArbitrationBitrate),   ArbitrationBitrate,   throwIfMissing: false);
            DataBitrate          = XmlAttributeParser.Get(xElement, nameof(DataBitrate),          DataBitrate,          throwIfMissing: false);
            EnableCustomBitrate  = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),  EnableCustomBitrate,  throwIfMissing: false);
            CustomBitrateContent = XmlAttributeParser.Get(xElement, nameof(CustomBitrateContent), CustomBitrateContent, throwIfMissing: false);
            Mode                 = XmlAttributeParser.Get(xElement, nameof(Mode),                 Mode,                 throwIfMissing: false);
            EnableA120Ohm        = XmlAttributeParser.Get(xElement, nameof(EnableA120Ohm),        EnableA120Ohm,        throwIfMissing: false);
            EnableFilter         = XmlAttributeParser.Get(xElement, nameof(EnableFilter),         EnableFilter,         throwIfMissing: false);
            ExtendedFilter       = XmlAttributeParser.Get(xElement, nameof(ExtendedFilter),       ExtendedFilter,       throwIfMissing: false);
            Filter_FromId        = XmlAttributeParser.Get(xElement, nameof(Filter_FromId),        Filter_FromId,        throwIfMissing: false);
            Filter_ToId          = XmlAttributeParser.Get(xElement, nameof(Filter_ToId),          Filter_ToId,          throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(DeviceModel), DeviceModel));
            xelement.Add(new XAttribute(nameof(DeviceIndex), DeviceIndex));
            xelement.Add(new XAttribute(nameof(DeviceChannel), DeviceChannel));
            xelement.Add(new XAttribute(nameof(CANFDStandard), CANFDStandard));
            xelement.Add(new XAttribute(nameof(UseCanFD), UseCanFD));
            xelement.Add(new XAttribute(nameof(ArbitrationBitrate), ArbitrationBitrate));
            xelement.Add(new XAttribute(nameof(DataBitrate), DataBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(CustomBitrateContent), CustomBitrateContent));
            xelement.Add(new XAttribute(nameof(Mode), Mode));
            xelement.Add(new XAttribute(nameof(EnableA120Ohm), EnableA120Ohm));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(ExtendedFilter), ExtendedFilter));
            xelement.Add(new XAttribute(nameof(Filter_FromId), Filter_FromId));
            xelement.Add(new XAttribute(nameof(Filter_ToId), Filter_ToId));
            return xelement;
        }
    }
}
