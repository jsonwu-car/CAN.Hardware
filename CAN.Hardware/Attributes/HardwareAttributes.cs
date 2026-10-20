namespace CAN.Hardware.Attributes
{
    /// <summary>
    /// 硬件定义特性：以 厂商 -> 设备类型 -> 通信协议 三层枚举描述一个硬件封装。
    /// 设备类型必须使用对应厂商的枚举（见 <see cref="EquipmentTypeMap"/>），否则构造函数抛出异常。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class HardwareDefAttributes : Attribute
    {
        /// <summary>
        /// 厂商 -> 设备类型枚举 的对应关系
        /// </summary>
        private static readonly IReadOnlyDictionary<HardwareManufacturer, Type> EquipmentTypeMap =
            new Dictionary<HardwareManufacturer, Type>
            {
                { HardwareManufacturer.Kvaser, typeof(KvaserDeviceType) },
                { HardwareManufacturer.Peak, typeof(PeakDeviceType) },
                { HardwareManufacturer.Tosun, typeof(TosunDeviceType) },
                { HardwareManufacturer.Zlg, typeof(ZlgDeviceType) },
            };

        public HardwareManufacturer Manufacturer { get; }

        /// <summary>
        /// 设备类型枚举值（实际类型为对应厂商的枚举，如 <see cref="KvaserDeviceType"/>）
        /// </summary>
        public Enum EquipmentType { get; }

        public CommProtocol CommProtocol { get; }

        public HardwareDefAttributes(HardwareManufacturer manufacturer, object equipmentType, CommProtocol commProtocol)
        {
            if (equipmentType is not Enum equipment)
                throw new ArgumentException("equipmentType 必须是枚举值", nameof(equipmentType));
            if (EquipmentTypeMap.TryGetValue(manufacturer, out Type? expectedType) && equipment.GetType() != expectedType)
                throw new ArgumentException($"厂商 {manufacturer} 的设备类型必须使用枚举 {expectedType.Name}", nameof(equipmentType));

            Manufacturer = manufacturer;
            EquipmentType = equipment;
            CommProtocol = commProtocol;
        }
    }
}
