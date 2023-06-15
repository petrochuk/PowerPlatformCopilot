using System.Text.Json.Serialization;

namespace DataverseCopilot.Intent;

[DebuggerDisplay("{Name}")]
public class IntentAction
{
    public const string Iterate = "Iterate";
    public const string Details = "Details";
    public const string EmailReply = "Reply";
    public const string EmailForward = "Forward";
    public const string EmailDelete = "Delete";

    public IntentAction()
    {
        Name = string.Empty;
        Aliases = Array.Empty<IntentActionAlias>();
    }

    public IntentAction(string name, IReadOnlyList<IntentActionAlias> aliases)
    {
        Name = name;
        Aliases = aliases;
    }

    public IntentAction(string name, IReadOnlyList<string>? aliases = null)
    {
        Name = name;
        var aliasesList = new List<IntentActionAlias>();
        foreach (var alias in aliases ?? Array.Empty<string>())
        {
            aliasesList.Add(new (alias, null));
        }

        Aliases = aliasesList;
    }

    public string Name { get; set; }

    public IReadOnlyList<IntentActionAlias> Aliases { get; set; }
}

[DebuggerDisplay("{Alias}")]
public class IntentActionAlias 
{
    [JsonConstructor]
    private IntentActionAlias()
    {
        Alias = string.Empty;
    }

    public IntentActionAlias(string alias, IReadOnlyList<float>? vector = null)
    {
        Alias = alias;
        Vector = vector;
    }

    public string Alias { get; set; }

    public IReadOnlyList<float>? Vector { get; set; }
}