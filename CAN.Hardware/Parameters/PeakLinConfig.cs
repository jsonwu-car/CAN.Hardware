using System.ComponentModel;
using System.Xml.Linq;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// PEAK LIN(PLIN API)配置。
    /// PLIN 用“可用硬件列表下标”选择 LIN 通道，校验方式由帧条目/发送报文逐帧指定。
    /// </summary>
    public class PeakLinConfig : LinConfigBase
    {
        private uint _hardwareIndex = 0;
        private bool _checksumType = false;

        [Description("LIN硬件索引(可用硬件列表中的序号,从0开始)")]
        public uint HardwareIndex
        {
            get => _hardwareIndex;
            set => SetProperty(ref _hardwareIndex, value);
        }

        [Description("LIN校验和类型,false=经典校验,true=增强校验")]
        public bool ChecksumType
        {
            get => _checksumType;
            set => SetProperty(ref _checksumType, value);
        }

        public PeakLinConfig()
        {
        }

        public override void Init(XElement xElement)
        {
            base.Init(xElement);
            HardwareIndex = XmlAttributeParser.Get(xElement, nameof(HardwareIndex), HardwareIndex, throwIfMissing: false);
            ChecksumType = XmlAttributeParser.Get(xElement, nameof(ChecksumType), ChecksumType, throwIfMissing: false);
        }

        public override XElement ToXElement(string NodeName)
        {
            XElement xElement = base.ToXElement(NodeName);
            xElement.Add(new XAttribute(nameof(HardwareIndex), HardwareIndex));
            xElement.Add(new XAttribute(nameof(ChecksumType), ChecksumType));
            return xElement;
        }
    }
}
