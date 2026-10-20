using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    public class ZlgCANConfig : BusConfigBase, IXmlSerializer
    {
        private ZlgDeviceModel _deviceModel = ZlgDeviceModel.USBCAN_2E_U;
        private uint _deviceIndex = 0;
        private uint _deviceChannel = 0;
        private ZlgCanBitrate _canBitrate = ZlgCanBitrate._500K;
        private bool _enableCustomBitrate = false;
        // 自定义波特率内容: "波特率(采样点),(Timing0,Timing1)" 格式
        private string _customBitrateContent = "500Kbps(75%),(01,14)";
        private bool _enableA120Ohm = false;
        private bool _enableFilter = false;
        private uint _filter_FromId = 0;
        private uint _filter_ToId = 0;
        [Description("ZLG设备型号")]
        public ZlgDeviceModel DeviceModel
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
        [Description("CAN波特率")]
        public ZlgCanBitrate CanBitrate
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
        [Description("自定义波特率内容(如500Kbps(75%),(01,14),括号内为Timing0/Timing1)")]
        public string CustomBitrateContent
        {
            get => _customBitrateContent;
            set => SetProperty(ref _customBitrateContent, value);
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
        // ZLG 过滤器使用 ID 区间(filter_start~filter_end),而非掩码方式
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
        public ZlgCANConfig() { }
        public void Init(XElement xElement)
        {
            DeviceModel          = XmlAttributeParser.Get(xElement, nameof(DeviceModel),          DeviceModel,          throwIfMissing: false);
            DeviceIndex          = XmlAttributeParser.Get(xElement, nameof(DeviceIndex),          DeviceIndex,          throwIfMissing: false);
            DeviceChannel        = XmlAttributeParser.Get(xElement, nameof(DeviceChannel),        DeviceChannel,        throwIfMissing: false);
            CanBitrate           = XmlAttributeParser.Get(xElement, nameof(CanBitrate),           CanBitrate,           throwIfMissing: false);
            EnableCustomBitrate  = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate),  EnableCustomBitrate,  throwIfMissing: false);
            CustomBitrateContent = XmlAttributeParser.Get(xElement, nameof(CustomBitrateContent), CustomBitrateContent, throwIfMissing: false);
            EnableA120Ohm        = XmlAttributeParser.Get(xElement, nameof(EnableA120Ohm),        EnableA120Ohm,        throwIfMissing: false);
            EnableFilter         = XmlAttributeParser.Get(xElement, nameof(EnableFilter),         EnableFilter,         throwIfMissing: false);
            Filter_FromId        = XmlAttributeParser.Get(xElement, nameof(Filter_FromId),        Filter_FromId,        throwIfMissing: false);
            Filter_ToId          = XmlAttributeParser.Get(xElement, nameof(Filter_ToId),          Filter_ToId,          throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(DeviceModel), DeviceModel));
            xelement.Add(new XAttribute(nameof(DeviceIndex), DeviceIndex));
            xelement.Add(new XAttribute(nameof(DeviceChannel), DeviceChannel));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(CustomBitrateContent), CustomBitrateContent));
            xelement.Add(new XAttribute(nameof(EnableA120Ohm), EnableA120Ohm));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(Filter_FromId), Filter_FromId));
            xelement.Add(new XAttribute(nameof(Filter_ToId), Filter_ToId));
            return xelement;
        }
    }
}
