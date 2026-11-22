using System.ComponentModel;
using System.Xml.Linq;
using CAN.Hardware.Comm;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 周立功(ZLG) LIN 配置。
    /// 复用 zlgcan.dll 的设备/通道两层句柄；LIN 通道型号、校验方式、最大长度与
    /// 内部上拉终端电阻由本类描述。
    /// </summary>
    public class ZlgLinConfig : LinConfigBase
    {
        private ZlgDeviceModel _deviceModel = ZlgDeviceModel.USBCANFD_200U;
        private uint _deviceIndex = 0;
        private uint _deviceChannel = 0;
        private ZlgLinChkSumMode _chkSumMode = ZlgLinChkSumMode.Auto;
        private byte _maxLength = 8;
        private bool _enableInternalResistor = true;

        [Description("ZLG设备型号(LIN 通道通常为 USBCANFD_200U)")]
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

        [Description("ZLG设备LIN通道号(0基)")]
        public uint DeviceChannel
        {
            get => _deviceChannel;
            set => SetProperty(ref _deviceChannel, value);
        }

        [Description("LIN校验方式:Classic=经典,Enhanced=增强,Auto=自动")]
        public ZlgLinChkSumMode ChkSumMode
        {
            get => _chkSumMode;
            set => SetProperty(ref _chkSumMode, value);
        }

        [Description("最大数据长度(8~64,ZLG 初始化参数)")]
        public byte MaxLength
        {
            get => _maxLength;
            set => SetProperty(ref _maxLength, value);
        }

        [Description("使能LIN内部上拉终端电阻(主机模式需要)")]
        public bool EnableInternalResistor
        {
            get => _enableInternalResistor;
            set => SetProperty(ref _enableInternalResistor, value);
        }

        public ZlgLinConfig()
        {
        }

        public override void Init(XElement xElement)
        {
            base.Init(xElement);
            DeviceModel            = XmlAttributeParser.Get(xElement, nameof(DeviceModel),            DeviceModel,            throwIfMissing: false);
            DeviceIndex            = XmlAttributeParser.Get(xElement, nameof(DeviceIndex),            DeviceIndex,            throwIfMissing: false);
            DeviceChannel          = XmlAttributeParser.Get(xElement, nameof(DeviceChannel),          DeviceChannel,          throwIfMissing: false);
            ChkSumMode             = XmlAttributeParser.Get(xElement, nameof(ChkSumMode),             ChkSumMode,             throwIfMissing: false);
            MaxLength              = XmlAttributeParser.Get(xElement, nameof(MaxLength),              MaxLength,              throwIfMissing: false);
            EnableInternalResistor = XmlAttributeParser.Get(xElement, nameof(EnableInternalResistor), EnableInternalResistor, throwIfMissing: false);
        }

        public override XElement ToXElement(string NodeName)
        {
            XElement xElement = base.ToXElement(NodeName);
            xElement.Add(new XAttribute(nameof(DeviceModel), DeviceModel));
            xElement.Add(new XAttribute(nameof(DeviceIndex), DeviceIndex));
            xElement.Add(new XAttribute(nameof(DeviceChannel), DeviceChannel));
            xElement.Add(new XAttribute(nameof(ChkSumMode), ChkSumMode));
            xElement.Add(new XAttribute(nameof(MaxLength), MaxLength));
            xElement.Add(new XAttribute(nameof(EnableInternalResistor), EnableInternalResistor));
            return xElement;
        }
    }
}
