namespace DataverseCopilot.Graph;

internal static class GraphExtensions
{
    public static string CleanupSubject(this string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return subject;

        return subject.Replace("RE: ", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
