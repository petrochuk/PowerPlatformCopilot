namespace PowerAppGenerator;

public static class StringExtensions
{
    public static string? ExtractJsonArray(this string input)
    {
        var start = input.IndexOf('[');
        var end = input.LastIndexOf(']');

        if (start < 0 || end < 0)
            return null;

        return input.Substring(start, end - start + 1);
    }
}
