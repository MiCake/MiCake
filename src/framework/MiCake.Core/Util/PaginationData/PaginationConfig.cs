using System;

namespace MiCake.Core.Util.PaginationData
{
    /// <summary>
    /// Generic pagination configuration
    /// </summary>
    public class PaginationConfig
    {
        private int _maxItemsPerRequest = 1000;
        private int _maxRequests = 50;
        private int _delayBetweenRequests = 100;

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
        /// Maximum number of requests to make to the data source.
        /// Default is 50.
        /// </summary>
        public int MaxRequests 
        { 
            get => _maxRequests; 
            set => _maxRequests = value <= 0 ? throw new ArgumentOutOfRangeException(nameof(value), "MaxRequests must be greater than 0") : value; 
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
    }
}