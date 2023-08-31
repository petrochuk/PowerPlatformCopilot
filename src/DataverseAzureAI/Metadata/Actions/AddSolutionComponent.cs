namespace AP2.DataverseAzureAI.Metadata.Actions;

public class AddSolutionComponent
{
    public required string SolutionUniqueName { get; set; }
    public required string ComponentId { get; set; }
    public required int ComponentType { get; set; }
    public bool AddRequiredComponents { get; set; } = false;
    public bool DoNotIncludeSubcomponents { get; set; } = false;
}
