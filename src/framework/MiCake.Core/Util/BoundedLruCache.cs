using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MiCake.Core.Util
{
    /// <summary>
    /// A thread-safe bounded LRU (Least Recently Used) cache implementation.
    /// Designed for MiCake framework's lightweight and performance-conscious architecture.
    /// </summary>
    /// <typeparam name="TKey">The type of cache keys</typeparam>
    /// <typeparam name="TValue">The type of cache values</typeparam>
    public sealed class BoundedLruCache<TKey, TValue> : IDisposable
    {
        private readonly int _maxSize;
        private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _accessOrder;
        private readonly object _lock = new();
        private volatile bool _disposed;

        /// <summary>
        /// Initialize a new bounded LRU cache
        /// </summary>
        /// <param name="maxSize">Maximum number of items to cache (default: 1000)</param>
        public BoundedLruCache(int maxSize = 1000)
        {
            if (maxSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be positive");

            _maxSize = maxSize;
            _cache = new ConcurrentDictionary<TKey, LinkedListNode<CacheItem>>();
            _accessOrder = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// Get or add a value to the cache using the provided factory function
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="valueFactory">Factory function to create the value if not cached</param>
        /// <returns>The cached or newly created value</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BoundedLruCache<TKey, TValue>));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            // Fast path: try to get existing value
            if (_cache.TryGetValue(key, out var existingNode))
            {
                // Move to front (most recently used)
                MoveToFront(existingNode);
                return existingNode.Value.Value;
            }

            // Slow path: create new value and add to cache
            var newValue = valueFactory(key);
            AddOrUpdate(key, newValue);
            return newValue;
        }

        /// <summary>
        /// Try to get a value from the cache
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="value">The cached value if found</param>
        /// <returns>True if value was found in cache</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BoundedLruCache<TKey, TValue>));

            if (_cache.TryGetValue(key, out var node))
            {
                MoveToFront(node);
                value = node.Value.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Add or update a value in the cache
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="value">The value to cache</param>
        public void AddOrUpdate(TKey key, TValue value)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BoundedLruCache<TKey, TValue>));

            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (_lock)
            {
                if (_disposed)
                    return;

                // Check if key already exists (double-check after lock)
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    // Update existing value and move to front
                    existingNode.Value.Value = value;
                    _accessOrder.Remove(existingNode);
                    _accessOrder.AddFirst(existingNode);
                    return;
                }

                // Create new cache item
                var cacheItem = new CacheItem(key, value);
                var newNode = new LinkedListNode<CacheItem>(cacheItem);

                // Add to front of access order list (most recently used)
                _accessOrder.AddFirst(newNode);
                _cache.TryAdd(key, newNode);

                // Evict least recently used items if over capacity
                while (_accessOrder.Count > _maxSize)
                {
                    var lruNode = _accessOrder.Last;
                    if (lruNode != null)
                    {
                        _accessOrder.RemoveLast();
                        _cache.TryRemove(lruNode.Value.Key, out _);
                    }
                }
            }
        }

        /// <summary>
        /// Remove a value from the cache
        /// </summary>
        /// <param name="key">The cache key to remove</param>
        /// <returns>True if the key was found and removed</returns>
        public bool Remove(TKey key)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BoundedLruCache<TKey, TValue>));

            if (key == null)
                return false;

            lock (_lock)
            {
                if (_disposed)
                    return false;

                if (_cache.TryRemove(key, out var node))
                {
                    _accessOrder.Remove(node);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Clear all cached items
        /// </summary>
        public void Clear()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                _cache.Clear();
                _accessOrder.Clear();
            }
        }

        /// <summary>
        /// Get current cache size
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Get maximum cache size
        /// </summary>
        public int MaxSize => _maxSize;

        /// <summary>
        /// Check if the cache contains a specific key
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists in cache</returns>
        public bool ContainsKey(TKey key)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BoundedLruCache<TKey, TValue>));

            return _cache.ContainsKey(key);
        }

        private void MoveToFront(LinkedListNode<CacheItem> node)
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_disposed || node.List != _accessOrder)
                    return;

                // Move to front (most recently used)
                _accessOrder.Remove(node);
                _accessOrder.AddFirst(node);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;
                    
                _disposed = true;
                
                // Clear cache after setting disposed flag
                _cache.Clear();
                _accessOrder.Clear();
            }
        }

        /// <summary>
        /// Internal cache item structure
        /// </summary>
        private sealed class CacheItem
        {
            public TKey Key { get; }
            public TValue Value { get; set; }

            public CacheItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}