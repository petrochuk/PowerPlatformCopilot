using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

public class UpdatePermissionRequest
{
    [JsonPropertyName("put")]
    public List<CanvasPermission> Put { get; set; } = new();
    [JsonPropertyName("delete")]
    public List<CanvasPermission> Delete { get; set; } = new();
}
