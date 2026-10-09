using System;
using System.ComponentModel;
using System.Reflection;

namespace CAN.Hardware.Comm
{
    internal static class EnumDescription
    {
        public static string GetEnumDescription(this Enum @enum)
        {
            FieldInfo fieldInfo = @enum.GetType().GetField(@enum.ToString());
            DescriptionAttribute descriptionAttribute = Attribute.GetCustomAttribute(fieldInfo,typeof(DescriptionAttribute)) as DescriptionAttribute;
            return descriptionAttribute != null ? descriptionAttribute.Description : @enum.ToString();
        }
    }
}
