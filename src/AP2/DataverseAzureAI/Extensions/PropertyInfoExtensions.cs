using System.Reflection;
using AP2.DataverseAzureAI.Metadata;

namespace AP2.DataverseAzureAI.Extensions;

public static class PropertyInfoExtensions
{
    public static bool Equals(this PropertyInfo propertyInfo, object instance, string? value)
    {
        _ = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        _ = instance ?? throw new ArgumentNullException(nameof(instance));

        var propertyValue = propertyInfo.GetValue(instance);
        if (propertyValue == null)
            return value == null;
        else if (value == null)
            return false;

        if (propertyInfo.PropertyType == typeof(string))
            return string.Equals((string)propertyValue, value.Trim(), StringComparison.OrdinalIgnoreCase);

        if (propertyInfo.PropertyType == typeof(DateTime))
        {
            return ((DateTime)propertyValue).RelativeEquals(value);
        }

        // Fall back to ToString() for any other types
        return string.Equals(propertyValue.ToString(), value.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static string GetValue(this PropertyInfo propertyInfo, object instance, string? customerFullName)
    {
        _ = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        _ = instance ?? throw new ArgumentNullException(nameof(instance));

        var propertyValue = propertyInfo.GetValue(instance);
        if (propertyValue == null)
            return $"{propertyInfo.Name}: null";

        if (propertyInfo.PropertyType == typeof(DateTime))
            return $"{propertyInfo.Name}: {((DateTime)propertyValue).ToRelativeSentence()}";

        if (string.Compare(propertyValue.ToString(), customerFullName, StringComparison.OrdinalIgnoreCase) == 0)
            return $"{propertyInfo.Name}: me";

        return $"{propertyInfo.Name}: {propertyValue}";
    }
}
