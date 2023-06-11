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
            if(subject.StartsWith("RE: ", StringComparison.OrdinalIgnoreCase))
                subject = subject.Substring(4);
            else if (subject.StartsWith("FW: ", StringComparison.OrdinalIgnoreCase))
                subject = subject.Substring(4);
            else
                break;
        }

        return subject;
    }

    public static string ToEmbedding(this Message message)
    {
        var sb = new StringBuilder();

        sb.Append("From:");
        sb.AppendLine(message.From.EmailAddress.Name);
        sb.Append("From email:");
        sb.AppendLine(message.From.EmailAddress.Address);
        sb.Append("Subject:");
        sb.AppendLine(message.Subject.CleanupSubject());

        return sb.ToString();
    }
}
