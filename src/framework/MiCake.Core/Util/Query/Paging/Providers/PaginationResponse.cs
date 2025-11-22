using System.Collections.Generic;

namespace MiCake.Util.Query.Paging.Providers;

/// <summary>
/// Pagination response with data and continuation info
/// </summary>
public class PaginationResponse<TData>
{
    /// <summary>
    /// Retrieved data for the current request
    /// </summary>
    public List<TData>? Data { get; set; }

    /// <summary>
    /// Indicates whether there are more data available for the next request
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Offset to use for the next request
    /// </summary>
    public int? NextOffset { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}