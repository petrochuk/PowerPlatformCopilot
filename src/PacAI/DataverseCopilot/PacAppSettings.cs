namespace DataverseCopilot;

public class PacAppSettings
{
    public Uri? DataverseEnvironmentUri { get; set; }
    public string? OpenApiEndPoint { get; set; }
    public string? OpenApiKey { get; set; }
    public string? OpenApiModel { get; set; }
    public string? OpenApiEmbeddingsEndPoint { get; set; }
    public string? OpenApiEmbeddingsKey { get; set; }
    public string? OpenApiEmbeddingsModel { get; set; }
    public bool UseCompletionAPI { get; set; } = true;
    public string? AzureAppId { get; set; }
}
