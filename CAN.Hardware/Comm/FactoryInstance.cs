using CAN.Hardware.Attributes;
using CAN.Hardware.Interface;

namespace CAN.Hardware.Comm
{
    /// <summary>
    /// 硬件实例工厂：根据 厂商 -> 设备类型 -> 协议 三层枚举，
    /// 通过 <see cref="HardwareDefAttributes"/> 反射查找并创建对应的 <see cref="IHardware"/> 实现。
    /// </summary>
    public static class FactoryInstance
    {
        public static IHardware? GetInstance(HardwareManufacturer manufacturer, Enum equipmentType, CommProtocol commProtocol)
        {
            var hardwareAssembly = typeof(IHardware).Assembly;

            foreach (var type in hardwareAssembly.GetTypes())
            {
                if (!typeof(IHardware).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                    continue;

                foreach (var attr in type.GetCustomAttributes(typeof(HardwareDefAttributes), false).Cast<HardwareDefAttributes>())
                {
                    if (attr.Manufacturer == manufacturer
                        && attr.EquipmentType.Equals(equipmentType)
                        && attr.CommProtocol == commProtocol)
                    {
                        // nonPublic: true 兼容后续将 wrapper 改为 internal 的情况
                        return Activator.CreateInstance(type, nonPublic: true) as IHardware;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取当前程序集内已注册的硬件清单：厂商 -> 设备类型 -> 协议集合
        /// </summary>
        public static Dictionary<HardwareManufacturer, Dictionary<Enum, HashSet<CommProtocol>>> GetHardwareMap()
        {
            var hardwareMap = new Dictionary<HardwareManufacturer, Dictionary<Enum, HashSet<CommProtocol>>>();
            var hardwareAssembly = typeof(IHardware).Assembly;

            foreach (var type in hardwareAssembly.GetTypes())
            {
                if (!typeof(IHardware).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                    continue;

                foreach (var attr in type.GetCustomAttributes(typeof(HardwareDefAttributes), false).Cast<HardwareDefAttributes>())
                {
                    if (!hardwareMap.TryGetValue(attr.Manufacturer, out var eqDict))
                    {
                        eqDict = new Dictionary<Enum, HashSet<CommProtocol>>();
                        hardwareMap[attr.Manufacturer] = eqDict;
                    }
                    if (!eqDict.TryGetValue(attr.EquipmentType, out var protoSet))
                    {
                        protoSet = new HashSet<CommProtocol>();
                        eqDict[attr.EquipmentType] = protoSet;
                    }
                    protoSet.Add(attr.CommProtocol);
                }
            }
            return hardwareMap;
        }
    }
}
