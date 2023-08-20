using System.Net;
using System.Net.Http.Headers;
using Microsoft.OData;

namespace AP2.DataverseAzureAI.OData;

public class MessageWrapper : IODataRequestMessage, IODataResponseMessage, IODataResponseMessageAsync
{
    private readonly Stream _stream;
    private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();

    public MessageWrapper(Stream stream, HttpHeaders? headers = null)
    {
        _stream = stream;
        if (headers != null)
        {
            _headers = headers.ToDictionary(kvp => kvp.Key, kvp => string.Join(";", kvp.Value));
        }
    }

    public IEnumerable<KeyValuePair<string, string>> Headers => _headers;

    public string Method { get; set; } = HttpMethod.Get.Method;

    public Uri Url { get; set; } = default!;

    public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

    public string? GetHeader(string headerName)
    {
        if (_headers.TryGetValue(headerName, out var value))
            return value;

        return null;
    }

    public Stream GetStream()
    {
        return _stream;
    }

    public Task<Stream> GetStreamAsync()
    {
        return Task.FromResult(_stream);
    }

    public void SetHeader(string headerName, string headerValue)
    {
        _headers.Add(headerName, headerValue);
    }
}
