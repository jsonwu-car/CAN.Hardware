using System.Xml.Linq;

namespace CAN.Hardware.Interface
{
    /// <summary>
    /// 用于配置的本地序列化，亦可直接使用Json
    /// </summary>
    public interface IXmlSerializer
    {
        void Init(XElement xElement);
        XElement ToXElement(string NodeName);
    }
}
