using System;
using System.Net;

namespace MiCake.Core.Util.PaginationData;

/// <summary>
/// Context information passed to self-healing handlers
/// </summary>
public class SelfHealingContext
{
    /// <summary>
    /// The exception that triggered the self-healing attempt
    /// </summary>
    public Exception Exception { get; set; } = null!;

    /// <summary>
    /// The HTTP status code if available (null for non-HTTP errors)
    /// </summary>
    public HttpStatusCode? StatusCode { get; set; }

    /// <summary>
    /// Current retry attempt number (1-based)
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// The pagination request that failed
    /// </summary>
    public PaginationRequest<HttpPaginationRequest> Request { get; set; } = null!;

    /// <summary>
    /// Parsed response data if available
    /// </summary>
    public object? ResponseData { get; set; }

    /// <summary>
    /// User-defined state that can be used across retry attempts
    /// </summary>
    public object? State { get; set; }

    /// <summary>
    /// Whether to continue with retry after self-healing
    /// </summary>
    public bool ContinueRetry { get; set; } = true;

    /// <summary>
    /// Whether self-healing was successful
    /// </summary>
    public bool HealingSuccessful { get; set; }

    /// <summary>
    /// Additional message or reason from self-healing handler
    /// </summary>
    public string? HealingMessage { get; set; }
}

/// <summary>
/// Result of a self-healing attempt
/// </summary>
public class SelfHealingResult
{
    /// <summary>
    /// Whether the self-healing was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Whether to continue with retry after self-healing
    /// </summary>
    public bool ContinueRetry { get; set; } = true;

    /// <summary>
    /// Message describing the healing action taken
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// User state to pass to next retry attempt
    /// </summary>
    public object? State { get; set; }

    /// <summary>
    /// Creates a successful healing result
    /// </summary>
    public static SelfHealingResult Success(string? message = null, object? state = null) => new()
    {
        IsSuccessful = true,
        ContinueRetry = true,
        Message = message,
        State = state
    };

    /// <summary>
    /// Creates a failed healing result
    /// </summary>
    public static SelfHealingResult Failed(string? message = null, bool continueRetry = false) => new()
    {
        IsSuccessful = false,
        ContinueRetry = continueRetry,
        Message = message
    };

    /// <summary>
    /// Creates a result that stops further retries
    /// </summary>
    public static SelfHealingResult StopRetry(string? message = null) => new()
    {
        IsSuccessful = false,
        ContinueRetry = false,
        Message = message
    };
}
