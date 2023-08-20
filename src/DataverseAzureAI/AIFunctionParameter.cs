using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI;

public class AIFunctionParameter
{
    [JsonPropertyName("type")]
    public required string TypeName { get; set; }
    [JsonPropertyName("description")]
    public required string Description { get; set; }
}
