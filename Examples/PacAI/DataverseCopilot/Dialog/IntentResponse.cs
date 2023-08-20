namespace DataverseCopilot.Dialog;

[DebuggerDisplay("{Action} {ResourceObject.Name} filter:{Filter}")]
public class IntentResponse
{
    public const string ObjectKey = "object:";
    public const string FilterKey = "filter:";
    public const string ActionKey = "action:";

    public IntentResponse(string response, IReadOnlyCollection<Resource> resources)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new ArgumentNullException(nameof(response));

        foreach (var resource in resources)
        {
            if (response.Contains($"{ObjectKey} {resource.Name}", StringComparison.OrdinalIgnoreCase) ||
                response.Contains($"{ObjectKey}{resource.Name}", StringComparison.OrdinalIgnoreCase))
            {
                ResourceObject = resource;
            }
        }

        int filterIndex = response.IndexOf(FilterKey, StringComparison.OrdinalIgnoreCase);
        if (0 <= filterIndex)
        {
            int endIndex = response.IndexOfAny(new char[] { '\n', ',' }, filterIndex);
            if (endIndex < 0)
                endIndex = response.Length;

            filterIndex += FilterKey.Length;
            Filter = response.Substring(filterIndex, endIndex - filterIndex).Trim();
        }


        int actionIndex = response.IndexOf(ActionKey, StringComparison.OrdinalIgnoreCase);
        if (0 <= actionIndex)
        {
            int endIndex = response.IndexOfAny(new char[] { '\n', ',' }, actionIndex);
            if (endIndex < 0)
                endIndex = response.Length-1;

            actionIndex += ActionKey.Length;
            Action = response.Substring(actionIndex, endIndex - actionIndex).Trim();
        }
    }

    public Resource? ResourceObject { get; set; }

    public string? Filter { get; set; }

    public string? Action { get; set; }
}
