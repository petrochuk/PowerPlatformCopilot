using System.Diagnostics;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

[DebuggerDisplay("{ComponentType} {ObjectId}")]
public class SolutionComponent
{
    [JsonPropertyName("_modifiedby_value")]
    public string ModifiedBySystemUserId { get; set; }
    public DateTime modifiedon { get; set; }

    public DateTime createdon { get; set; }
    [JsonPropertyName("_createdby_value")]
    public string CreatedBySystemUserId { get; set; }

    public Guid Objectid { get; set; }
    public Guid SolutionComponentId { get; set; }
    public SolutionComponentType? ComponentType { get; set; }
    public int? RootComponentBehavior { get; set; }
    public int VersionNumber { get; set; }
    public bool IsMetadata { get; set; }
    public string _solutionid_value { get; set; }
    public string _createdonbehalfby_value { get; set; }
    public string _modifiedonbehalfby_value { get; set; }
    public string rootsolutioncomponentid { get; set; }
}
