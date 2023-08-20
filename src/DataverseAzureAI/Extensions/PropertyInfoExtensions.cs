using System.ComponentModel;
using System.Reflection;
using System.Text;
using AP2.DataverseAzureAI.Metadata;

namespace AP2.DataverseAzureAI.Extensions;

public static class PropertyInfoExtensions
{
    public static bool Equals(this PropertyInfo propertyInfo, object instance, string? value, TimeProvider timeProvider)
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
            return new DateTimeOffset(((DateTime)propertyValue)).RelativeEquals(value, timeProvider);
        else if (propertyInfo.PropertyType == typeof(DateTimeOffset))
            return ((DateTimeOffset)propertyValue).RelativeEquals(value, timeProvider);

        // Fall back to ToString() for any other types
        return string.Equals(propertyValue.ToString(), value.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static string GetValue(this PropertyInfo propertyInfo, object instance, string? customerFullName, TimeProvider timeProvider)
    {
        _ = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        _ = instance ?? throw new ArgumentNullException(nameof(instance));

        var propertyValue = propertyInfo.GetValue(instance);
        if (propertyValue == null)
            return $"{propertyInfo.Name}: null";

        if (propertyInfo.PropertyType == typeof(DateTime))
            return $"{propertyInfo.Name}: {(new DateTimeOffset((DateTime)propertyValue)).ToRelativeSentence(timeProvider)}";
        else if (propertyInfo.PropertyType == typeof(DateTimeOffset))
            return $"{propertyInfo.Name}: {((DateTimeOffset)propertyValue).ToRelativeSentence(timeProvider)}";

        if (string.Compare(propertyValue.ToString(), customerFullName, StringComparison.OrdinalIgnoreCase) == 0)
            return $"{propertyInfo.Name}: me";

        return $"{propertyInfo.Name}: {propertyValue}";
    }

    public static int CompareTo(this PropertyInfo propertyInfo, object left, object right)
    {
        _ = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
        _ = left ?? throw new ArgumentNullException(nameof(left));
        _ = right ?? throw new ArgumentNullException(nameof(right));

        var leftValue = propertyInfo.GetValue(left);
        var rightValue = propertyInfo.GetValue(right);
        if (leftValue == null && rightValue == null)
            return 0;
        if (leftValue == null)
            return -1;
        if (rightValue == null)
            return -1;
        if (propertyInfo.PropertyType == typeof(DateTime))
            return ((DateTime)leftValue).CompareTo((DateTime)rightValue);
        else if (propertyInfo.PropertyType == typeof(string))
            return string.Compare((string)leftValue, (string)rightValue, StringComparison.OrdinalIgnoreCase);
        else if (propertyInfo.PropertyType == typeof(int))
            return ((int)leftValue).CompareTo((int)rightValue);

        return string.Compare(leftValue.ToString(), rightValue.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public static string ToBrowsableProperties(this IReadOnlyCollection<PropertyInfo> properties)
    {
        _ = properties ?? throw new ArgumentNullException(nameof(properties));

        var sb = new StringBuilder();
        foreach (var property in properties)
        {
            var browsableAttribute = property.GetCustomAttribute<BrowsableAttribute>();
            if (browsableAttribute != null && !browsableAttribute.Browsable)
                continue;

            if (sb.Length <= 0)
                sb.Append(property.Name);
            else
                sb.Append($", {property.Name}");
        }
        return sb.ToString();
    }
}
