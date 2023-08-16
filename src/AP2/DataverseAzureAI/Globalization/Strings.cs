namespace AP2.DataverseAzureAI.Globalization;

public static class Strings
{
    public static Dictionary<string, string> Last = new(StringComparer.OrdinalIgnoreCase);

    static Strings()
    {
        Last.Add("Last", "Last");
        Last.Add("Latest", "Last");
        Last.Add("Recent", "Last");
        Last.Add("Recently", "Last");
        Last.Add("Newest", "Last");
        Last.Add("New", "Last");
    }
}
