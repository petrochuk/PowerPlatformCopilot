using AP2.DataverseAzureAI.OData;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

public class Solution
{
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

    [JsonIgnore]
    public Dictionary<Guid, SolutionComponent>? Components { get; set; }

    public void LoadComponents()
    {
        if (Components != null)
            return;

        /*
        _canvasApps = new Lazy<Task<IList<CanvasApp>>>(async () =>
        {
            if (EnvironmentId == Guid.Empty)
                throw new InvalidOperationException($"{nameof(EnvironmentId)} is not set.");

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildApiQueryUri($"powerapps/apps?%24expand=permissions%28%24filter%3DmaxAssignedTo%28%27{UserObjectId}%27%29%29&%24filter=classification+eq+%27SharedWithMeApps%27+and+environment+eq+%27{EnvironmentId}%27&api-version=1"));
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var appModules = JsonSerializer.Deserialize<ODataContext<CanvasApp>>(contentStream, _jsonSerializerOptions);
            if (appModules == null)
                throw new InvalidOperationException("Failed to get list of PowerApps.");
            return appModules.Values;
        });
        */
    }

    public override string ToString()
    {
        return FriendlyName;
    }
}
