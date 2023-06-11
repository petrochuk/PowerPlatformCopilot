namespace DataverseCopilot.Graph;

internal static class GraphExtensions
{
    public static string CleanupSubject(this string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return subject;

        while (true)
        {
            if(subject.StartsWith("RE: ", StringComparison.OrdinalIgnoreCase))
                subject = subject.Substring(4);
            else if (subject.StartsWith("FW: ", StringComparison.OrdinalIgnoreCase))
                subject = subject.Substring(4);
            else
                break;
        }

        return subject;
    }
}
