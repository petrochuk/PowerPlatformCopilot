using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

public class SolutionCreate
{
    [JsonPropertyName("uniquename")]
    public required string UniqueName { get; set; }
    
    [JsonPropertyName("friendlyname")]
    public required string FriendlyName { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0.0";

    [JsonPropertyName("publisherid@odata.bind")]
    public string PublisherId { get; set; } = "/publishers(00000001-0000-0000-0000-00000000005a)"; // CDS Default Publisher
}
