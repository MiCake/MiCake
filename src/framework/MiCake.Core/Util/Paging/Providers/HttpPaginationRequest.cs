using System.Collections.Generic;
using System.Net.Http;

namespace MiCake.Util.Paging.Providers;

/// <summary>
/// HTTP-specific pagination request
/// </summary>
public class HttpPaginationRequest
{
    public string BaseUrl { get; set; } = string.Empty;
    public Dictionary<string, string>? QueryParameters { get; set; }
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string? Body { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}