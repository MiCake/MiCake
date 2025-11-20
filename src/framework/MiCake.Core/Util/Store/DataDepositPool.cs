using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using MiCake.Util.Collection;
using MiCake.Util.Reflection;

namespace MiCake.Util.Store
{
    /// <summary>
    /// Type used to store transient data with capacity limits.
    /// Can release data by <see cref="Release"/> method.
    /// </summary>
    public class DataDepositPool : IDisposable
    {
        private const int DefaultMaxCapacity = 1000;
        private bool _isDispose = false;
        private readonly ConcurrentDictionary<string, object> _cachePool = new();
        private readonly int _maxCapacity;
        private readonly Lock _syncLock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDepositPool"/> class.
        /// </summary>
        /// <param name="maxCapacity">Maximum number of items that can be stored. Default is 1000.</param>
        public DataDepositPool(int maxCapacity = DefaultMaxCapacity)
        {
            if (maxCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Maximum capacity must be greater than zero.");

            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// Gets the current number of items in the pool.
        /// </summary>
        public int Count => _cachePool.Count;

        /// <summary>
        /// Gets the maximum capacity of the pool.
        /// </summary>
        public int MaxCapacity => _maxCapacity;

        /// <summary>
        /// Get the stored data according to the key
        /// </summary>
        /// <param name="key">The key to retrieve data for</param>
        /// <returns>The stored object if found; otherwise, null</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null</exception>
        public object? TakeOut(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!_cachePool.TryGetValue(key, out var result))
                return default;

            return result;
        }

        /// <summary>
        /// Get the stored data according to the key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T? TakeOut<T>(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!_cachePool.TryGetValue(key, out var result))
                return default;

            return (T)result;
        }


        /// <summary>
        /// Get the stored data according to the key type
        /// </summary>
        /// <param name="type">The type of the stored data</param>
        /// <returns></returns>
        public List<object> TakeOutByType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            var results = new List<object>();

            foreach (var item in _cachePool.Values)
            {
                if (TypeHelper.IsInheritedFrom(item.GetType(), type))
                {
                    results.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Deposit required data.
        /// </summary>
        /// <param name="key">The unique key for the data</param>
        /// <param name="dataInfo">Data information to store</param>
        /// <param name="isReplace">Whether to replace the existing data with the same key</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the key already exists or capacity is exceeded</exception>
        public void Deposit(string key, object dataInfo, bool isReplace = false)
        {
            ArgumentNullException.ThrowIfNull(key);

            lock (_syncLock)
            {
                if (_cachePool.Count >= _maxCapacity)
                {
                    throw new InvalidOperationException(
                        $"DataDepositPool capacity exceeded. Maximum capacity: {_maxCapacity}, current count: {_cachePool.Count}. " +
                        $"Please increase the capacity or remove existing items before adding new ones.");
                }

                if (!isReplace)
                {
                    if (_cachePool.ContainsKey(key))
                        throw new InvalidOperationException(
                            $"The key '{key}' already exists in DataDepositPool. " +
                            $"Please remove the existing item first using TakeOut or Release before adding a new one.");
                }

                _cachePool[key] = dataInfo;
            }
        }

        /// <summary>
        /// Releases all data from the pool.
        /// </summary>
        public void Release()
        {
            _cachePool.Clear();
        }

        void IDisposable.Dispose()
        {
            if (_isDispose)
                throw new InvalidOperationException($"{nameof(DataDepositPool)} has already been disposed.");

            _isDispose = true;

            Release();
        }
    }
}
