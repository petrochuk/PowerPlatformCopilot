using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

/// <summary>
/// Power Platform Solution
/// </summary>
public class Solution
{
    [Browsable(false)]
    public static Dictionary<string, PropertyInfo> Properties { get; }
        = typeof(Solution).GetProperties().ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public DateTime InstalledOn { get; set; }
    public string SolutionPackageVersion { get; set; }
    public string _configurationpageid_value { get; set; }
    public Guid SolutionId { get; set; }
    public string UniqueName { get; set; }
    public bool IsApiManaged { get; set; }
    public string _publisherid_value { get; set; }
    public bool IsManaged { get; set; }
    public bool IsVisible { get; set; }
    public object thumbprint { get; set; }
    public object pinpointpublisherid { get; set; }
    public string Version { get; set; }
    public object _modifiedonbehalfby_value { get; set; }
    public string _parentsolutionid_value { get; set; }
    public object pinpointassetid { get; set; }
    public object pinpointsolutionid { get; set; }
    public string FriendlyName { get; set; }
    public string _organizationid_value { get; set; }
    public int VersionNumber { get; set; }
    public object templatesuffix { get; set; }
    public string upgradeinfo { get; set; }
    public object _createdonbehalfby_value { get; set; }

    public DateTime? UpdatedOn { get; set; }
    public string Description { get; set; }
    public int? SolutionType { get; set; }
    public object pinpointsolutiondefaultlocale { get; set; }

    public DateTime CreatedOn { get; set; }
    public SystemUser CreatedBy { get; set; }
    [JsonPropertyName("_createdby_value")]
    public string CreatedBySystemUserId { get; set; }

    public DateTime ModifiedOn { get; set; }
    public SystemUser ModifiedBy { get; set; }
    [JsonPropertyName("_modifiedby_value")]
    public string ModifiedBySystemUserId { get; set; }

    [JsonIgnore, Browsable(false)]
    public Dictionary<SolutionComponentType, Dictionary<Guid, SolutionComponent>>? Components { get; set; }

    public override string ToString()
    {
        return FriendlyName;
    }
}
