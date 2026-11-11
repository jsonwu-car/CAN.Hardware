using System.ComponentModel;
using System.Xml.Linq;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// Kvaser LIN(linlib)配置。
    /// </summary>
    public class KvaserLinConfig : LinConfigBase
    {
        private uint _deviceChannel = 0;
        private bool _checksumType = false;
        private bool _variable_DLC = true;

        [Description("LIN设备通道号")]
        public uint DeviceChannel
        {
            get => _deviceChannel;
            set => SetProperty(ref _deviceChannel, value);
        }

        [Description("LIN校验和类型，false=经典校验，true=增强校验")]
        public bool ChecksumType
        {
            get => _checksumType;
            set => SetProperty(ref _checksumType, value);
        }

        [Description("启用可变DLC(帧长度由ID查表决定)")]
        public bool Variable_DLC
        {
            get => _variable_DLC;
            set => SetProperty(ref _variable_DLC, value);
        }

        public override void Init(XElement xElement)
        {
            base.Init(xElement);
            DeviceChannel = XmlAttributeParser.Get(xElement, nameof(DeviceChannel), DeviceChannel, throwIfMissing: false);
            ChecksumType = XmlAttributeParser.Get(xElement, nameof(ChecksumType), ChecksumType, throwIfMissing: false);
            Variable_DLC = XmlAttributeParser.Get(xElement, nameof(Variable_DLC), Variable_DLC, throwIfMissing: false);
        }

        public override XElement ToXElement(string NodeName)
        {
            XElement xelement = base.ToXElement(NodeName);
            xelement.Add(new XAttribute(nameof(DeviceChannel), DeviceChannel));
            xelement.Add(new XAttribute(nameof(ChecksumType), ChecksumType));
            xelement.Add(new XAttribute(nameof(Variable_DLC), Variable_DLC));
            return xelement;
        }
    }
}
