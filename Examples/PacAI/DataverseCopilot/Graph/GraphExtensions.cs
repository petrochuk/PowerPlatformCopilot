using Microsoft.Graph.Models;
using System.Text;

namespace DataverseCopilot.Graph;

internal static class GraphExtensions
{
    public static string CleanupSubject(this string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return subject;

        while (true)
        {
            if(subject.StartsWith("RE:", StringComparison.OrdinalIgnoreCase))
                subject = subject.Substring(3);
            else if (subject.StartsWith("FW:", StringComparison.OrdinalIgnoreCase))
                subject = subject.Substring(3);
            else if (subject.StartsWith("[EXTERNAL]", StringComparison.OrdinalIgnoreCase))
                subject = subject.Substring("[EXTERNAL]".Length);
            else
                break;

            subject = subject.TrimStart();
        }

        return subject;
    }

    public static string ToEmbedding(this Message message)
    {
        var sb = new StringBuilder();

        sb.AppendLine(message.From.EmailAddress.Name);
        sb.AppendLine(message.From.EmailAddress.Address);
        sb.AppendLine(message.Subject.CleanupSubject());
        sb.AppendLine(message.BodyPreview);

        return sb.ToString();
    }
}
