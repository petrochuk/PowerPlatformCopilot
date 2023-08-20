namespace DataverseCopilot;

public class AppSettings
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

    string? _azureAppIdForGraph;
    public string? AzureAppIdForGraph
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_azureAppIdForGraph))
                return AzureAppId;
            return _azureAppIdForGraph;
        }

        set
        {
            _azureAppIdForGraph = value;
        }
    }

    public string? SpeechSubscriptionKey { get; set; }
    public string? SpeechSubscriptionRegion { get; set; }
    public string? SpeechSynthesisVoiceName { get; set; }
}
