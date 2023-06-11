namespace DataverseCopilot.Dialog;

[DebuggerDisplay("{Intent} tgt:{Target.ToString()} src:{Source} flt:{Filter}")]
public class IntentResponse
{
    public const string IntentKey = $"{nameof(Intent)}:";
    public const string TargetKey = "target:";
    public const string SourceKey = "source:";
    public const string FilterKey = "filter:";

    public IntentResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentNullException(nameof(response));

        foreach (var intent in Enum.GetValues<Intent>())
        {
            if (response.Contains($"{IntentKey} {intent}", StringComparison.OrdinalIgnoreCase) ||
                response.Contains($"{IntentKey}{intent}", StringComparison.OrdinalIgnoreCase))
            {
                Intent = intent;
                break;
            }
        }

        foreach (var resource in Enum.GetValues<Resource>())
        {
            if (response.Contains($"{TargetKey} {resource}", StringComparison.OrdinalIgnoreCase) ||
                response.Contains($"{TargetKey}{resource}", StringComparison.OrdinalIgnoreCase))
            {
                Target = resource;
            }
            if (response.Contains($"{SourceKey} {resource}", StringComparison.OrdinalIgnoreCase) ||
                response.Contains($"{SourceKey}{resource}", StringComparison.OrdinalIgnoreCase))
            {
                Source = resource;
            }
        }

        int filterIndex = response.IndexOf(FilterKey, StringComparison.OrdinalIgnoreCase);
        if (0 <= filterIndex)
        {
            int endOfLine = response.IndexOf('\n', filterIndex);
            if (endOfLine < 0)
                endOfLine = response.Length;

            filterIndex += FilterKey.Length;
            Filter = response.Substring(filterIndex, endOfLine - filterIndex).Trim();
        }
    }

    public Intent Intent { get; private set; } = Intent.Unknown;

    public Resource? Target { get; set; }
    
    public Resource? Source { get; set; }

    public string? Filter { get; set; }
}
