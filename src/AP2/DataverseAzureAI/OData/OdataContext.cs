using System.Diagnostics;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.OData;

// OdataContext used for deserialization
#pragma warning disable CA2227 // Collection properties should be read only

/// <summary>
/// Helper to deserialize ODataContext
/// </summary>
[DebuggerDisplay("count: {Values.Count}")]
public class ODataContext<T>
{
    public ODataContext()
    {
        Values = new List<T>();
    }

    public ODataContext(IEnumerable<T> values)
    {
        Values = values.Cast<T>().ToList();
    }

    public ODataContext(T[] values)
    {
        Values = values;
    }

    [JsonPropertyName("@odata.context")]
    public string Context { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public IList<T> Values { get; set; }
}
