using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiCake.Util.Paging.Providers;

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
            if (config.MaxTotalItems > 0 && allData.Count >= config.MaxTotalItems)
            {
                _logger.LogDebug("Reached maximum total items limit: {MaxTotal} for {Identifier}",
                    config.MaxTotalItems, identifier);
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();

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
                    break;
                }

                if (response.Data == null || response.Data.Count == 0)
                {
                    _logger.LogDebug("No more data available for {Identifier} at offset {Offset}",
                        identifier, currentOffset);
                    break;
                }

                allData.AddRange(response.Data);
                hasMoreData = response.HasMore;
                currentPage++;

                // Update offset for next request
                if (response.NextOffset.HasValue)
                {
                    currentOffset = response.NextOffset.Value;
                }
                else
                {
                    currentOffset += response.Data.Count;
                }

                _logger.LogTrace("Fetched {Count} items in request {RequestCount} for {Identifier}, total: {Total}, hasMore: {HasMore}",
                    response.Data.Count, currentPage, identifier, allData.Count, hasMoreData);

                // Respect rate limiting
                if (hasMoreData && currentPage < config.MaxPages && config.DelayBetweenRequests > 0)
                {
                    await Task.Delay(config.DelayBetweenRequests, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching data for {Identifier} at offset {Offset}",
                    identifier, currentOffset);
                break;
            }
        }

        if (currentPage >= config.MaxPages)
        {
            _logger.LogWarning("Reached maximum request limit ({MaxPages}) for {Identifier}",
                config.MaxPages, identifier);
        }

        _logger.LogInformation("Completed paginated loading for {Identifier}: {TotalItems} items in {RequestCount} requests",
            identifier, allData.Count, currentPage);

        return allData.Count > 0 ? allData : null;
    }
}