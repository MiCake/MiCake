using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiCake.Util.Query.Paging.Providers;

/// <summary>
/// Abstract base class providing generic pagination functionality
/// </summary>
public abstract class PaginationDataProviderBase<TRequest, TData>
{
    protected readonly ILogger _logger;

    protected PaginationDataProviderBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Abstract method to fetch a single page of data
    /// Implementations can use HTTP, database queries, file I/O, etc.
    /// </summary>
    /// <param name="request">Pagination request with offset/limit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pagination response with data and continuation info</returns>
    protected abstract Task<PaginationResponse<TData>> FetchPageAsync(
        PaginationRequest<TRequest> request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generic pagination method for fetching all data
    /// </summary>
    /// <param name="initialRequest">Initial request parameters</param>
    /// <param name="config">Pagination configuration</param>
    /// <param name="identifier">Optional identifier for logging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all fetched data</returns>
    protected async Task<List<TData>?> LoadPaginatedDataAsync(
        TRequest initialRequest,
        PaginationConfig? config = null,
        string? identifier = null,
        CancellationToken cancellationToken = default)
    {
        config ??= new PaginationConfig();
        var allData = new List<TData>();
        int currentOffset = 0;
        bool hasMoreData = true;
        int currentPage = config.StartPageOffset ?? 0;

        _logger.LogDebug("Starting paginated data loading for {Identifier}, config: {@Config}",
            identifier ?? "unknown", config);

        while (hasMoreData && currentPage < config.MaxPages)
        {
            if (ShouldStopDueToItemLimit(config, allData.Count, identifier))
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var fetchResult = await FetchSinglePageAsync(
                initialRequest, config, identifier, currentOffset, currentPage, cancellationToken).ConfigureAwait(false);

            if (fetchResult.ShouldBreak)
            {
                break;
            }

            allData.AddRange(fetchResult.Data!);
            hasMoreData = fetchResult.HasMore;
            currentPage++;
            currentOffset = fetchResult.NextOffset;

            if (hasMoreData && currentPage < config.MaxPages && config.DelayBetweenRequests > 0)
            {
                await Task.Delay(config.DelayBetweenRequests, cancellationToken).ConfigureAwait(false);
            }
        }

        LogCompletionStatus(config, identifier, allData.Count, currentPage);

        return allData.Count > 0 ? allData : null;
    }

    /// <summary>
    /// Check if pagination should stop due to item limit
    /// </summary>
    private bool ShouldStopDueToItemLimit(PaginationConfig config, int currentCount, string? identifier)
    {
        if (config.MaxTotalItems > 0 && currentCount >= config.MaxTotalItems)
        {
            _logger.LogDebug("Reached maximum total items limit: {MaxTotal} for {Identifier}",
                config.MaxTotalItems, identifier);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Fetch a single page of data
    /// </summary>
    private async Task<PageFetchResult> FetchSinglePageAsync(
        TRequest initialRequest,
        PaginationConfig config,
        string? identifier,
        int currentOffset,
        int currentPage,
        CancellationToken cancellationToken)
    {
        try
        {
            var paginationRequest = new PaginationRequest<TRequest>
            {
                Request = initialRequest,
                Offset = currentOffset,
                Limit = config.MaxItemsPerRequest,
                Identifier = identifier
            };

            _logger.LogTrace("Making request {RequestCount} for {Identifier} at offset {Offset}",
                currentPage + 1, identifier, currentOffset);

            var response = await FetchPageAsync(paginationRequest, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccess)
            {
                _logger.LogWarning("Request failed for {Identifier} at offset {Offset}: {Error}",
                    identifier, currentOffset, response.ErrorMessage);
                return new PageFetchResult { ShouldBreak = true };
            }

            if (response.Data == null || response.Data.Count == 0)
            {
                _logger.LogDebug("No more data available for {Identifier} at offset {Offset}",
                    identifier, currentOffset);
                return new PageFetchResult { ShouldBreak = true };
            }

            var nextOffset = response.NextOffset ?? currentOffset + response.Data.Count;

            _logger.LogTrace("Fetched {Count} items in request {RequestCount} for {Identifier}, hasMore: {HasMore}",
                response.Data.Count, currentPage + 1, identifier, response.HasMore);

            return new PageFetchResult
            {
                ShouldBreak = false,
                Data = response.Data,
                HasMore = response.HasMore,
                NextOffset = nextOffset
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching data for {Identifier} at offset {Offset}",
                identifier, currentOffset);
            return new PageFetchResult { ShouldBreak = true };
        }
    }

    /// <summary>
    /// Log completion status of pagination
    /// </summary>
    private void LogCompletionStatus(PaginationConfig config, string? identifier, int totalItems, int currentPage)
    {
        if (currentPage >= config.MaxPages)
        {
            _logger.LogWarning("Reached maximum request limit ({MaxPages}) for {Identifier}",
                config.MaxPages, identifier);
        }

        _logger.LogInformation("Completed paginated loading for {Identifier}: {TotalItems} items in {RequestCount} requests",
            identifier, totalItems, currentPage);
    }

    /// <summary>
    /// Result of a single page fetch
    /// </summary>
    private struct PageFetchResult
    {
        public bool ShouldBreak { get; init; }
        public List<TData>? Data { get; init; }
        public bool HasMore { get; init; }
        public int NextOffset { get; init; }
    }
}