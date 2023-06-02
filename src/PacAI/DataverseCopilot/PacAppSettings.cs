namespace DataverseCopilot;

public class PacAppSettings
{
    public string? DataverseEnvironment { get; set; }
    public string? OpenApiEndPoint { get; set; }
    public string? OpenApiKey { get; set; }
    public string? OpenApiModel { get; set; }
    public bool UseCompletionAPI { get; set; } = true;
    public string? AzureAppId { get; set; }
}
