using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MiCake.Util.Cache
{
    /// <summary>
    /// Thread-safe bounded LRU cache with optional segmentation and lock-free approximation.
    /// Designed to provide predictable LRU semantics for small caches and high throughput for large concurrent caches.
    /// </summary>
    public class BoundedLruCache<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly int _maxSize;
        private readonly Segment[] _segments;
        private readonly int _segmentsCount;
        private volatile bool _disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="BoundedLruCache{TKey, TValue}"/>.
        /// </summary>
        /// <param name="maxSize">Total maximum number of cached entries across all segments (must be &gt; 0).</param>
        /// <param name="segments">Optional number of internal segments for striping. If <c>null</c> the implementation will pick a reasonable default based on CPU count.
        /// Small caches (maxSize &lt; 16) use a single segment to preserve deterministic LRU semantics.</param>
        /// <param name="useLockFreeApproximation">If true, each segment will use a lock-free, best-effort approximation of LRU (higher throughput but relaxed eviction ordering).</param>
        public BoundedLruCache(int maxSize = 1000, int? segments = null, bool useLockFreeApproximation = false)
        {
            if (maxSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be positive");

            _maxSize = maxSize;

            var defaultSegments = Math.Max(1, Environment.ProcessorCount);
            if (segments.HasValue)
            {
                _segmentsCount = Math.Min(Math.Max(1, segments.Value), Math.Max(1, maxSize));
            }
            else
            {
                // keep a single segment for small caches to preserve deterministic LRU semantics
                _segmentsCount = maxSize < 16 ? 1 : Math.Min(defaultSegments, Math.Max(1, maxSize));
            }

            _segments = new Segment[_segmentsCount];
            var baseCap = maxSize / _segmentsCount;
            var rem = maxSize % _segmentsCount;

            for (int i = 0; i < _segmentsCount; i++)
            {
                var segCap = baseCap + (i < rem ? 1 : 0);
                _segments[i] = new Segment(segCap, useLockFreeApproximation);
            }
        }

        private Segment LocateSegment(TKey key)
        {
            var hash = key.GetHashCode() & 0x7fffffff;
            return _segments[hash % _segmentsCount];
        }

        /// <summary>
        /// Gets the cached value for <paramref name="key"/> or adds a new value created by <paramref name="valueFactory"/>.
        /// </summary>
        /// <remarks>
        /// In the default (lock-based) mode, the factory is executed under a segment lock and is guaranteed to only be invoked once for the key
        /// during concurrent access. In lock-free mode the factory may be invoked multiple times during races (the stored value remains consistent),
        /// so prefer idempotent factories in that mode.
        /// </remarks>
        /// <param name="key">The key to find or add (must not be null).</param>
        /// <param name="valueFactory">Factory to create the value if the key is absent (must not be null).</param>
        /// <returns>The existing or newly added value.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(valueFactory);

            var seg = LocateSegment(key);
            if (seg.TryGetValue(key, out var existing))
                return existing;

            return seg.GetOrAdd(key, valueFactory);
        }

        /// <summary>
        /// Attempts to get the cached value for <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">When this method returns, contains the cached value if found; otherwise the default value for <typeparamref name="TValue"/>.</param>
        /// <returns><c>true</c> if the value was found; otherwise <c>false</c>.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(key);
            var seg = LocateSegment(key);
            return seg.TryGetValue(key, out value);
        }

        /// <summary>
        /// Adds an entry to the cache or updates the value of an existing entry.
        /// </summary>
        /// <param name="key">Cache key (must not be null).</param>
        /// <param name="value">Value to be stored for the key.</param>
        public void AddOrUpdate(TKey key, TValue value)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(key);
            var seg = LocateSegment(key);
            seg.AddOrUpdate(key, value);
        }

        /// <summary>
        /// Removes the given key from the cache if present.
        /// </summary>
        /// <param name="key">Key to remove (if null the method returns false).</param>
        /// <returns>True when the key was present and removed; otherwise false.</returns>
        public bool Remove(TKey key)
        {
            ThrowIfDisposed();

            if (key is null)
                return false;

            var seg = LocateSegment(key);
            return seg.Remove(key);
        }

        /// <summary>
        /// Removes all cached entries from the cache.
        /// </summary>
        public void Clear()
        {
            if (_disposed)
                return;

            foreach (var s in _segments)
                s.Clear();
        }

        /// <summary>
        /// Number of items currently stored in the cache across all segments.
        /// </summary>
        public int Count
        {
            get
            {
                var sum = 0;
                foreach (var s in _segments) sum += s.Count;
                return sum;
            }
        }

        /// <summary>
        /// Maximum number of items this cache will store across all segments.
        /// </summary>
        public int MaxSize => _maxSize;

        /// <summary>
        /// Determines whether the cache contains the specified key.
        /// </summary>
        /// <param name="key">Key to check.</param>
        /// <returns><c>true</c> when the key is present in the cache; otherwise <c>false</c>.</returns>
        public bool ContainsKey(TKey key)
        {
            ThrowIfDisposed();

            var seg = LocateSegment(key);
            return seg.ContainsKey(key);
        }

        /// <summary>
        /// Dispose the cache instance and free its internal resources. After calling this method, other operations will throw <see cref="ObjectDisposedException"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose pattern implementation.
        /// </summary>
        /// <param name="disposing">True when called from Dispose(); false when called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                foreach (var s in _segments)
                    s.Dispose();
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BoundedLruCache<TKey, TValue>));
        }

        private sealed class Segment : IDisposable
        {
            private readonly int _maxSize;
            private readonly bool _useLockFree;

            // lock-based
            private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> _cache = new();
            private readonly LinkedList<CacheItem> _accessOrder = new();
            private readonly object _segLock = new();

            // lock-free - uses timestamp-based eviction for accurate LRU tracking
            private readonly ConcurrentDictionary<TKey, TimestampedValue> _lfCache = new();
            private long _timestamp = 0;

            private volatile bool _disposed;

            public Segment(int maxSize, bool useLockFree)
            {
                _maxSize = Math.Max(0, maxSize);
                _useLockFree = useLockFree;
            }

            public int Count => _useLockFree ? _lfCache.Count : _cache.Count;

            public bool TryGetValue(TKey key, out TValue value)
            {
                ThrowIfDisposed();

                if (_useLockFree)
                {
                    if (_lfCache.TryGetValue(key, out var tsValue))
                    {
                        TouchLockFreeEntry(key, tsValue);
                        value = tsValue.Value;
                        return true;
                    }

                    value = default!;
                    return false;
                }

                if (_cache.TryGetValue(key, out var node))
                {
                    value = node.Value.Value;

                    lock (_segLock)
                    {
                        MoveNodeToFront(node);
                    }

                    return true;
                }

                value = default!;
                return false;
            }

            public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
            {
                ThrowIfDisposed();
                if (key is null) throw new ArgumentNullException(nameof(key));

                if (_useLockFree)
                {
                    if (_lfCache.TryGetValue(key, out var existing))
                    {
                        TouchLockFreeEntry(key, existing);
                        return existing.Value;
                    }

                    var ts = Interlocked.Increment(ref _timestamp);
                    var newValue = valueFactory(key);
                    if (!_lfCache.TryAdd(key, new TimestampedValue(newValue, ts)) && _lfCache.TryGetValue(key, out var raceValue))
                        return raceValue.Value;

                    TrimLockFreeIfNeeded();
                    return newValue;
                }

                lock (_segLock)
                {
                    if (_cache.TryGetValue(key, out var existingNode))
                    {
                        MoveNodeToFront(existingNode);
                        return existingNode.Value.Value;
                    }

                    var newValue = valueFactory(key);
                    AddNodeUnsafe(key, newValue);
                    TrimLockBasedIfNeeded();
                    return newValue;
                }
            }

            private KeyValuePair<TKey, TimestampedValue>? FindOldestItem()
            {
                KeyValuePair<TKey, TimestampedValue>? oldest = null;
                foreach (var item in _lfCache)
                {
                    if (oldest == null || item.Value.Timestamp < oldest.Value.Value.Timestamp)
                        oldest = item;
                }
                return oldest;
            }

            public void AddOrUpdate(TKey key, TValue value)
            {
                ThrowIfDisposed();

                if (_useLockFree)
                {
                    UpdateLockFreeEntry(key, value);
                    TrimLockFreeIfNeeded();
                    return;
                }

                lock (_segLock)
                {
                    if (_cache.TryGetValue(key, out var existingNode))
                    {
                        existingNode.Value.Value = value;
                        MoveNodeToFront(existingNode);
                        return;
                    }

                    AddNodeUnsafe(key, value);
                    TrimLockBasedIfNeeded();
                }
            }

            public bool Remove(TKey key)
            {
                ThrowIfDisposed();

                if (_useLockFree)
                    return _lfCache.TryRemove(key, out _);

                lock (_segLock)
                {
                    if (_cache.TryRemove(key, out var node))
                    {
                        _accessOrder.Remove(node);
                        return true;
                    }
                }

                return false;
            }

            public void Clear()
            {
                if (_useLockFree)
                {
                    _lfCache.Clear();
                }
                else
                {
                    lock (_segLock)
                    {
                        _cache.Clear();
                        _accessOrder.Clear();
                    }
                }
            }

            public bool ContainsKey(TKey key)
            {
                ThrowIfDisposed();
                return _useLockFree ? _lfCache.ContainsKey(key) : _cache.ContainsKey(key);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (_disposed) return;
                if (disposing)
                {
                    if (_useLockFree)
                    {
                        _lfCache.Clear();
                    }
                    else
                    {
                        lock (_segLock)
                        {
                            _cache.Clear();
                            _accessOrder.Clear();
                        }
                    }
                }

                _disposed = true;
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(BoundedLruCache<TKey, TValue>));
            }

            private void MoveNodeToFront(LinkedListNode<CacheItem> node)
            {
                if (node.List != _accessOrder || node == _accessOrder.First)
                    return;

                _accessOrder.Remove(node);
                _accessOrder.AddFirst(node);
            }

            private void AddNodeUnsafe(TKey key, TValue value)
            {
                var node = new LinkedListNode<CacheItem>(new CacheItem(key, value));
                _accessOrder.AddFirst(node);
                _cache[key] = node;
            }

            private void TrimLockBasedIfNeeded()
            {
                while (_accessOrder.Count > _maxSize && _accessOrder.Last is LinkedListNode<CacheItem> lru)
                {
                    _accessOrder.RemoveLast();
                    _cache.TryRemove(lru.Value.Key, out _);
                }
            }

            private void TrimLockFreeIfNeeded()
            {
                while (_lfCache.Count > _maxSize)
                {
                    if (TryRemoveOldestLockFree() || TryRemoveAnyLockFreeEntry())
                        continue;

                    if (_lfCache.IsEmpty)
                        break;
                }
            }

            private bool TryRemoveOldestLockFree()
            {
                var oldest = FindOldestItem();
                return oldest.HasValue && _lfCache.TryRemove(oldest.Value.Key, out _);
            }

            private bool TryRemoveAnyLockFreeEntry()
            {
                foreach (var entry in _lfCache)
                {
                    if (_lfCache.TryRemove(entry.Key, out _))
                        return true;
                }

                return false;
            }

            private void TouchLockFreeEntry(TKey key, TimestampedValue current)
            {
                var newTs = Interlocked.Increment(ref _timestamp);
                _lfCache.TryUpdate(key, new TimestampedValue(current.Value, newTs), current);
            }

            private void UpdateLockFreeEntry(TKey key, TValue value)
            {
                var ts = Interlocked.Increment(ref _timestamp);
                _lfCache.AddOrUpdate(key, new TimestampedValue(value, ts), (k, _) => new TimestampedValue(value, ts));
            }
        }

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

        private sealed class TimestampedValue
        {
            public TValue Value { get; }
            public long Timestamp { get; }

            public TimestampedValue(TValue value, long timestamp)
            {
                Value = value;
                Timestamp = timestamp;
            }
        }
    }
}