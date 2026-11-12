using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 爱泰科技(Itekon)经典 CAN 配置。
    /// 对应 ControlCAN.dll(VCI_ API),适用于 PCI/USB 经典 CAN 系列。
    /// </summary>
    public class ItekonCANConfig : BusConfigBase, IXmlSerializer
    {
        private ItekonCanDeviceModel _deviceModel = ItekonCanDeviceModel.USBCAN2;
        private uint _deviceIndex = 0;
        private uint _deviceChannel = 0;
        private ItekonCanBitrate _canBitrate = ItekonCanBitrate._500K;
        private bool _enableCustomBitrate = false;
        private byte _timing0 = 0x00;
        private byte _timing1 = 0x1C;
        private ItekonCanMode _mode = ItekonCanMode.Normal;
        private ItekonCanSendType _sendType = ItekonCanSendType.Normal;
        private bool _enableFilter = false;
        private ItekonCanFilterMode _filterMode = ItekonCanFilterMode.Dual;
        private uint _accCode = 0;
        private uint _accMask = 0xFFFFFFFF;

        [Description("爱泰经典 CAN 设备型号")]
        public ItekonCanDeviceModel DeviceModel
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
        public ItekonCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }

        [Description("启用自定义波特率(Timing0/Timing1)")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        [Description("自定义波特率 Timing0 寄存器值")]
        public byte Timing0
        {
            get => _timing0;
            set => SetProperty(ref _timing0, value);
        }

        [Description("自定义波特率 Timing1 寄存器值")]
        public byte Timing1
        {
            get => _timing1;
            set => SetProperty(ref _timing1, value);
        }

        [Description("工作模式(正常/只听)")]
        public ItekonCanMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        [Description("发送帧类型(正常/单次/自发自收/单次自发自收)")]
        public ItekonCanSendType SendType
        {
            get => _sendType;
            set => SetProperty(ref _sendType, value);
        }

        [Description("启用硬件过滤器(AccCode/AccMask)")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }

        [Description("滤波方式(双滤波/单滤波)")]
        public ItekonCanFilterMode FilterMode
        {
            get => _filterMode;
            set => SetProperty(ref _filterMode, value);
        }

        [Description("验收码(AccCode)")]
        public uint AccCode
        {
            get => _accCode;
            set => SetProperty(ref _accCode, value);
        }

        [Description("屏蔽码(AccMask)")]
        public uint AccMask
        {
            get => _accMask;
            set => SetProperty(ref _accMask, value);
        }

        public ItekonCANConfig() { }

        public void Init(XElement xElement)
        {
            DeviceModel         = XmlAttributeParser.Get(xElement, nameof(DeviceModel),         DeviceModel,         throwIfMissing: false);
            DeviceIndex         = XmlAttributeParser.Get(xElement, nameof(DeviceIndex),         DeviceIndex,         throwIfMissing: false);
            DeviceChannel       = XmlAttributeParser.Get(xElement, nameof(DeviceChannel),       DeviceChannel,       throwIfMissing: false);
            CanBitrate          = XmlAttributeParser.Get(xElement, nameof(CanBitrate),          CanBitrate,          throwIfMissing: false);
            EnableCustomBitrate = XmlAttributeParser.Get(xElement, nameof(EnableCustomBitrate), EnableCustomBitrate, throwIfMissing: false);
            Timing0             = XmlAttributeParser.Get(xElement, nameof(Timing0),             Timing0,             throwIfMissing: false);
            Timing1             = XmlAttributeParser.Get(xElement, nameof(Timing1),             Timing1,             throwIfMissing: false);
            Mode                = XmlAttributeParser.Get(xElement, nameof(Mode),                Mode,                throwIfMissing: false);
            SendType            = XmlAttributeParser.Get(xElement, nameof(SendType),            SendType,            throwIfMissing: false);
            EnableFilter        = XmlAttributeParser.Get(xElement, nameof(EnableFilter),        EnableFilter,        throwIfMissing: false);
            FilterMode          = XmlAttributeParser.Get(xElement, nameof(FilterMode),          FilterMode,          throwIfMissing: false);
            AccCode             = XmlAttributeParser.Get(xElement, nameof(AccCode),             AccCode,             throwIfMissing: false);
            AccMask             = XmlAttributeParser.Get(xElement, nameof(AccMask),             AccMask,             throwIfMissing: false);
        }

        public XElement ToXElement(string NodeName)
        {
            XElement xelement = new XElement(NodeName);
            xelement.Add(new XAttribute(nameof(DeviceModel), DeviceModel));
            xelement.Add(new XAttribute(nameof(DeviceIndex), DeviceIndex));
            xelement.Add(new XAttribute(nameof(DeviceChannel), DeviceChannel));
            xelement.Add(new XAttribute(nameof(CanBitrate), CanBitrate));
            xelement.Add(new XAttribute(nameof(EnableCustomBitrate), EnableCustomBitrate));
            xelement.Add(new XAttribute(nameof(Timing0), Timing0));
            xelement.Add(new XAttribute(nameof(Timing1), Timing1));
            xelement.Add(new XAttribute(nameof(Mode), Mode));
            xelement.Add(new XAttribute(nameof(SendType), SendType));
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(FilterMode), FilterMode));
            xelement.Add(new XAttribute(nameof(AccCode), AccCode));
            xelement.Add(new XAttribute(nameof(AccMask), AccMask));
            return xelement;
        }
    }
}
