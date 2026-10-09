using System.ComponentModel;
using System.Globalization;
using System.Xml.Linq;

namespace CAN.Hardware.Parameters
{
    /// <summary>
    /// 通用的 XML Attribute 解析器：支持各种类型的自动转换；
    /// 当属性缺失或为空时使用默认值；可通过参数控制是否抛出异常。
    /// </summary>
    public static class XmlAttributeParser
    {
        public static T Get<T>(XElement element, string attributeName, T defaultValue, bool throwIfMissing = false)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (string.IsNullOrWhiteSpace(attributeName)) throw new ArgumentNullException(nameof(attributeName));

            var attr = element.Attribute(attributeName);
            var raw = attr?.Value;

            if (string.IsNullOrWhiteSpace(raw))
            {
                if (throwIfMissing)
                    throw new ArgumentNullException(attributeName, $"Attribute '{attributeName}' is missing or empty.");
                return defaultValue;
            }

            try
            {
                return ConvertTo<T>(raw);
            }
            catch (Exception ex)
            {
                throw new FormatException($"Attribute '{attributeName}' value '{raw}' cannot be converted to {typeof(T).Name}: {ex.Message}");
            }
        }

        private static T ConvertTo<T>(string value)
        {
            var t = typeof(T);
            var isNullable = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
            var targetType = isNullable ? Nullable.GetUnderlyingType(t) : t;

            if (targetType == typeof(string))
                return (T)(object)value;

            if (targetType.IsEnum)
                return (T)Enum.Parse(targetType, value, ignoreCase: true);

            if (targetType == typeof(Guid))
                return (T)(object)Guid.Parse(value);

            if (targetType == typeof(TimeSpan))
                return (T)(object)TimeSpan.Parse(value, CultureInfo.InvariantCulture);

            var converter = TypeDescriptor.GetConverter(targetType);
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                var converted = converter.ConvertFromInvariantString(value);
                return (T)converted;
            }

            // 回退：使用 ChangeType（对基础类型有效）
            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
    }
}
