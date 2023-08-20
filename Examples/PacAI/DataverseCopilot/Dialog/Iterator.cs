namespace DataverseCopilot.Dialog;

/// <summary>
/// Part of dialog when user iterates through a list of objects
/// </summary>
public abstract class Iterator
{
    public int Index { get; set; }

    public Resource? ResourceObject { get; set; }

    public string? Filter { get; set; }

    public abstract Task<bool> Next();
}
