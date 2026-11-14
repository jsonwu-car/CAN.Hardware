using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 广成科技 USBCAN-I/II 经典 CAN 配置。
    /// 对应 ECanVci64.dll,设备类型由 <see cref="DeviceModel"/> 决定(3/4)。
    /// </summary>
    public class GuangChengCANConfig : BusConfigBase, IXmlSerializer
    {
        private GuangChengCanDeviceModel _deviceModel = GuangChengCanDeviceModel.USBCAN_II;
        private uint _deviceIndex = 0;
        private uint _deviceChannel = 0;
        private GuangChengCanBitrate _canBitrate = GuangChengCanBitrate._500K;
        private bool _enableCustomBitrate = false;
        private byte _timing0 = 0x00;
        private byte _timing1 = 0x1C;
        private GuangChengCanMode _mode = GuangChengCanMode.Normal;
        private GuangChengCanSendType _sendType = GuangChengCanSendType.Normal;
        private bool _enableFilter = false;
        private uint _accCode = 0;
        private uint _accMask = 0xFFFFFFFF;

        [Description("广成设备型号(USBCAN-I=3 / USBCAN-II=4)")]
        public GuangChengCanDeviceModel DeviceModel
        {
            get => _deviceModel;
            set => SetProperty(ref _deviceModel, value);
        }

        [Description("设备索引号(按 SN 从小到大分配)")]
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
        public GuangChengCanBitrate CanBitrate
        {
            get => _canBitrate;
            set => SetProperty(ref _canBitrate, value);
        }

        [Description("启用自定义波特率(直接使用 Timing0/Timing1)")]
        public bool EnableCustomBitrate
        {
            get => _enableCustomBitrate;
            set => SetProperty(ref _enableCustomBitrate, value);
        }

        [Description("自定义波特率定时器0(BTR0)")]
        public byte Timing0
        {
            get => _timing0;
            set => SetProperty(ref _timing0, value);
        }

        [Description("自定义波特率定时器1(BTR1)")]
        public byte Timing1
        {
            get => _timing1;
            set => SetProperty(ref _timing1, value);
        }

        [Description("工作模式(正常/只听/自发自收)")]
        public GuangChengCanMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        [Description("发送类型(正常/单次/自发自收/单次自发自收)")]
        public GuangChengCanSendType SendType
        {
            get => _sendType;
            set => SetProperty(ref _sendType, value);
        }

        [Description("启用硬件滤波器(SJA1000 验收码/屏蔽码)")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }

        [Description("验收码(AccCode,由厂商 CANTest 工具计算)")]
        public uint AccCode
        {
            get => _accCode;
            set => SetProperty(ref _accCode, value);
        }

        [Description("屏蔽码(AccMask,不启用滤波时推荐 0xFFFFFFFF)")]
        public uint AccMask
        {
            get => _accMask;
            set => SetProperty(ref _accMask, value);
        }

        public GuangChengCANConfig() { }

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
            xelement.Add(new XAttribute(nameof(AccCode), AccCode));
            xelement.Add(new XAttribute(nameof(AccMask), AccMask));
            return xelement;
        }
    }
}
