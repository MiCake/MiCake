using System;

namespace MiCake.Util.Query.Paging.Providers
{
    /// <summary>
    /// Generic pagination configuration
    /// </summary>
    public class PaginationConfig
    {
        private int _maxItemsPerRequest = 1000;
        private int _maxPages = 50;
        private int _delayBetweenRequests = 100;
        private int? _startPageOffset;

        /// <summary>
        /// Maximum number of items to retrieve per request. Default is 1000.
        /// </summary>
        public int MaxItemsPerRequest
        {
            get => _maxItemsPerRequest;
            set => _maxItemsPerRequest = value <= 0 ? throw new ArgumentOutOfRangeException(nameof(value), "MaxItemsPerRequest must be greater than 0") : value;
        }

        /// <summary>
        /// Maximum total items to retrieve. Default is 0 (no limit).
        /// </summary>
        public int MaxTotalItems { get; set; } = 0; // 0 = no limit

        /// <summary>
        /// Maximum number of requests to make. This helps prevent excessive pagination.
        /// Default is 50.
        /// </summary>
        public int MaxPages
        {
            get => _maxPages;
            set => _maxPages = value <= 0 ? throw new ArgumentOutOfRangeException(nameof(value), "MaxPages must be greater than 0") : value;
        }

        /// <summary>
        /// Delay between requests in milliseconds to avoid overwhelming the data source.
        /// Default is 100 milliseconds.
        /// </summary>
        public int DelayBetweenRequests
        {
            get => _delayBetweenRequests;
            set => _delayBetweenRequests = value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "DelayBetweenRequests must be greater than or equal to 0") : value;
        }
        
        /// <summary>
        /// Optional starting offset for pagination. Default is null (start from the beginning).
        /// </summary>
        public int? StartPageOffset
        {
            get => _startPageOffset;
            set => _startPageOffset = value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "StartPageOffset must be greater than or equal to 0") : value;
        }
    }
}