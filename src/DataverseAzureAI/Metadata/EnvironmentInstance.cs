namespace AP2.DataverseAzureAI.Metadata;

public class EnvironmentInstance
{
    public bool IsUserSysAdmin { get; set; }
    public string Region { get; set; }
    public string Purpose { get; set; }
    public int StatusMessage { get; set; }
    public DateTime TrialExpirationDate { get; set; }
    public int OrganizationType { get; set; }
    public required string TenantId { get; set; }
    public required string EnvironmentId { get; set; }
    public string DatacenterId { get; set; }
    public object DatacenterName { get; set; }
    public required string Id { get; set; }
    public string UniqueName { get; set; }
    public string UrlName { get; set; }
    public string FriendlyName { get; set; }
    public int State { get; set; }
    public string Version { get; set; }
    public Uri Url { get; set; }
    public Uri ApiUrl { get; set; }
    public DateTime LastUpdated { get; set; }
    public string SchemaType { get; set; }
}
