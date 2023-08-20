namespace AP2.DataverseAzureAI.Settings;

public class AzureAISettings
{
    public required Uri OpenApiEndPoint { get; set; }
    public string? OpenApiKey { get; set; }
    public required string OpenApiModel { get; set; }
}
