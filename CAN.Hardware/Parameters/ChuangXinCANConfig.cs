using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;
using CAN.Hardware.Interface;
using CAN.Hardware.Model;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 创芯科技经典 CAN 分析仪配置。
    /// 适用于 USBCAN-2A/2C、CANalyst-II 至尊版/顶配/顶配Pro、OBD 版(ControlCAN.dll)。
    /// </summary>
    public class ChuangXinCANConfig : BusConfigBase, IXmlSerializer
    {
        private ChuangXinCanDeviceModel _deviceModel = ChuangXinCanDeviceModel.USBCAN2;
        private uint _deviceIndex = 0;
        private uint _deviceChannel = 0;
        private ChuangXinCanBitrate _canBitrate = ChuangXinCanBitrate._500K;
        private bool _enableCustomBitrate = false;
        private byte _timing0 = 0x00;
        private byte _timing1 = 0x1C;
        private ChuangXinCanMode _mode = ChuangXinCanMode.Normal;
        private bool _enableFilter = false;
        private bool _extendedFilter = false;
        private uint _accCode = 0;
        private uint _accMask = 0xFFFFFFFF;

        [Description("创芯设备型号(CANalyst-II/USBCAN-2A/2C 常用 USBCAN2)")]
        public ChuangXinCanDeviceModel DeviceModel
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
        public ChuangXinCanBitrate CanBitrate
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

        [Description("工作模式(正常/只听/自测)")]
        public ChuangXinCanMode Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        [Description("启用硬件过滤器")]
        public bool EnableFilter
        {
            get => _enableFilter;
            set => SetProperty(ref _enableFilter, value);
        }

        [Description("过滤器使用扩展帧(仅 EnableFilter=true 时有效)")]
        public bool ExtendedFilter
        {
            get => _extendedFilter;
            set => SetProperty(ref _extendedFilter, value);
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

        public ChuangXinCANConfig() { }

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
            EnableFilter        = XmlAttributeParser.Get(xElement, nameof(EnableFilter),        EnableFilter,        throwIfMissing: false);
            ExtendedFilter      = XmlAttributeParser.Get(xElement, nameof(ExtendedFilter),      ExtendedFilter,      throwIfMissing: false);
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
            xelement.Add(new XAttribute(nameof(EnableFilter), EnableFilter));
            xelement.Add(new XAttribute(nameof(ExtendedFilter), ExtendedFilter));
            xelement.Add(new XAttribute(nameof(AccCode), AccCode));
            xelement.Add(new XAttribute(nameof(AccMask), AccMask));
            return xelement;
        }
    }
}
