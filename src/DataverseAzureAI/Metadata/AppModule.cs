using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

[DebuggerDisplay("{Name}")]
public class AppModule
{
    public static Dictionary<string, PropertyInfo> Properties
    { get; } = typeof(AppModule).GetProperties().ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("statecode@OData.Community.Display.V1.FormattedValue")]
    public string StateCodeFormattedValue { get; set; }

    public int StateCode { get; set; }

    public string UniqueName { get; set; }

    public string WebResourceId { get; set; }

    [JsonPropertyName("createdon@OData.Community.Display.V1.FormattedValue")]
    public string CreatedOnFormattedValue { get; set; }

    public DateTime CreatedOn { get; set; }

    [JsonPropertyName("_createdby_value@OData.Community.Display.V1.FormattedValue")]
    public string CreatedByFormattedValue { get; set; }

    public string _createdby_valueMicrosoftDynamicsCRMlookuplogicalname { get; set; }

    [JsonPropertyName("_createdby_value")]
    public string _createdby_value { get; set; }

    public string Name { get; set; }

    public string modifiedonODataCommunityDisplayV1FormattedValue { get; set; }

    public DateTime? ModifiedOn { get; set; }

    public string componentstateODataCommunityDisplayV1FormattedValue { get; set; }

    public int componentstate { get; set; }

    public string ismanagedODataCommunityDisplayV1FormattedValue { get; set; }

    public bool IsManaged { get; set; }

    public Guid AppmoduleId { get; set; }

    [JsonPropertyName("publishedon@OData.Community.Display.V1.FormattedValue")]
    public string PublishedOnFormattedValue { get; set; }

    public DateTime? PublishedOn { get; set; }
}
