using System.ComponentModel;

namespace SmartPackageHub.Domains.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var enumType = value.GetType();
            var name = Enum.GetName(enumType, value);
            if (name == null)
            {
                return string.Empty;
            }

            var field = enumType.GetField(name);
            if (field is null)
            {
                return string.Empty;
            }

            var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute?.Description ?? string.Empty;
        }
    }
}
