using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Reflection;

namespace DataverseCopilot.Extensions
{
    public static class EnumExtensions
    {
        public static string DescriptionAttr<T>(this T source)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            var fi = source.GetType().GetField(source.ToString());

            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0) 
                return attributes[0].Description;
            
            return source.ToString();
        }

        public static List<string>? GetListOfDescriptions(this Type source)
        {
            if (!source.IsEnum)
                return null;

            return Enum.GetValues(source).Cast<Enum>().Select(x => x.DescriptionAttr()).ToList();
        }

        public static string GetDescriptions(this Type source)
        {
            var list = source.GetListOfDescriptions();
            if (list == null)
                return string.Empty;

            return string.Join(",", list);
        }
    }
}
