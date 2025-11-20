using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace MiCake.Util.Reflection
{
    /// <summary>
    /// Provides high-performance object instantiation using compiled expressions.
    /// Much faster than Activator.CreateInstance for repeated instantiations.
    /// </summary>
    public static class CompiledActivator
    {
        private const int MaxParameterizedCacheSize = 256;

        private static readonly ConcurrentDictionary<Type, Func<object>> _factoryCache = new();
        private static readonly ConcurrentDictionary<string, CacheEntry> _parameterizedFactoryCache = new();
        private static readonly LinkedList<string> _lruList = new();
        private static readonly ReaderWriterLockSlim _lruLock = new();

        private class CacheEntry
        {
            public Func<object[], object> Factory { get; set; } = null!;
            public LinkedListNode<string> LruNode { get; set; } = null!;
        }

        /// <summary>
        /// Creates an instance of the specified type using a cached compiled factory.
        /// </summary>
        /// <param name="type">The type to instantiate</param>
        /// <returns>A new instance of the specified type</returns>
        /// <exception cref="ArgumentNullException">Thrown when type is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the type has no parameterless constructor</exception>
        public static object CreateInstance(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            var factory = _factoryCache.GetOrAdd(type, CreateFactory);
            return factory();
        }

        /// <summary>
        /// Creates an instance of the specified type with constructor parameters.
        /// </summary>
        /// <param name="type">The type to instantiate</param>
        /// <param name="args">Constructor arguments</param>
        /// <returns>A new instance of the specified type</returns>
        public static object CreateInstance(Type type, params object[] args)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (args == null || args.Length == 0)
                return CreateInstance(type);

            var argTypes = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
                argTypes[i] = args[i]?.GetType() ?? typeof(object);

            var cacheKey = BuildCacheKey(type, argTypes);

            var factory = GetOrAddParameterizedFactory(cacheKey, type, argTypes);
            return factory(args);
        }

        /// <summary>
        /// Creates a compiled factory function for the specified type.
        /// </summary>
        private static Func<object> CreateFactory(Type type)
        {
            var constructor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"Type '{type.FullName}' does not have a parameterless constructor. " +
                    $"Ensure the type has a public or internal parameterless constructor.");
            }

            var newExpression = Expression.New(constructor);
            var lambda = Expression.Lambda<Func<object>>(
                Expression.Convert(newExpression, typeof(object)));

            return lambda.Compile();
        }

        /// <summary>
        /// Creates a compiled factory function for the specified type with parameters.
        /// </summary>
        private static Func<object[], object> CreateParameterizedFactory(Type type, Type[] parameterTypes)
        {
            var constructor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                parameterTypes,
                null);

            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"Type '{type.FullName}' does not have a constructor with parameters: {string.Join(", ", parameterTypes.Select(t => t.Name))}");
            }

            var argsParameter = Expression.Parameter(typeof(object[]), "args");
            var parameterExpressions = constructor.GetParameters()
                .Select((p, i) => Expression.Convert(
                    Expression.ArrayIndex(argsParameter, Expression.Constant(i)),
                    p.ParameterType))
                .ToArray();

            var newExpression = Expression.New(constructor, parameterExpressions);
            var lambda = Expression.Lambda<Func<object[], object>>(
                Expression.Convert(newExpression, typeof(object)),
                argsParameter);

            return lambda.Compile();
        }

        /// <summary>
        /// Clears the factory cache. Useful for testing or memory management.
        /// </summary>
        internal static void ClearCache()
        {
            _factoryCache.Clear();
            _parameterizedFactoryCache.Clear();

            _lruLock.EnterWriteLock();
            try
            {
                _lruList.Clear();
            }
            finally
            {
                _lruLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the current number of items in the parameterized cache.
        /// </summary>
        internal static int GetParameterizedCacheSize()
        {
            return _parameterizedFactoryCache.Count;
        }

        /// <summary>
        /// Gets the current number of items in the non-parameterized cache.
        /// </summary>
        internal static int GetFactoryCacheSize()
        {
            return _factoryCache.Count;
        }

        private static string BuildCacheKey(Type type, Type[] argTypes)
        {
            // Pre-calculate capacity to avoid reallocations
            var capacity = type?.FullName?.Length + 1; // +1 for underscore
            for (int i = 0; i < argTypes.Length; i++)
            {
                capacity += argTypes[i]?.FullName?.Length ?? 0;
                if (i < argTypes.Length - 1)
                    capacity += 1; // underscore between types
            }

            var sb = new StringBuilder(capacity ?? 0);
            sb.Append(type?.FullName);
            sb.Append('_');

            for (int i = 0; i < argTypes.Length; i++)
            {
                if (i > 0)
                    sb.Append('_');
                sb.Append(argTypes[i].FullName);
            }

            return sb.ToString();
        }

        private static Func<object[], object> GetOrAddParameterizedFactory(string cacheKey, Type type, Type[] parameterTypes)
        {
            // Fast path - try to get from cache first
            if (_parameterizedFactoryCache.TryGetValue(cacheKey, out var entry))
            {
                // Update LRU - move to end
                _lruLock.EnterWriteLock();
                try
                {
                    if (entry.LruNode != null)
                    {
                        _lruList.Remove(entry.LruNode);
                        _lruList.AddLast(entry.LruNode);
                    }
                }
                finally
                {
                    _lruLock.ExitWriteLock();
                }

                return entry.Factory;
            }

            // Slow path - create new factory and add to cache
            var factory = CreateParameterizedFactory(type, parameterTypes);
            var newEntry = new CacheEntry { Factory = factory };

            _lruLock.EnterWriteLock();
            try
            {
                // Double-check pattern
                if (_parameterizedFactoryCache.TryGetValue(cacheKey, out var existingEntry))
                {
                    return existingEntry.Factory;
                }

                // Add to LRU list
                var node = _lruList.AddLast(cacheKey);
                newEntry.LruNode = node;

                // Add to cache
                _parameterizedFactoryCache.TryAdd(cacheKey, newEntry);

                // Check if we need to evict
                if (_parameterizedFactoryCache.Count > MaxParameterizedCacheSize)
                {
                    EvictLruEntry();
                }
            }
            finally
            {
                _lruLock.ExitWriteLock();
            }

            return factory;
        }

        private static void EvictLruEntry()
        {
            // Assumes caller holds write lock on _lruLock
            if (_lruList.Count == 0)
                return;

            var lruNode = _lruList.First;
            _lruList.RemoveFirst();

            var cacheKey = lruNode?.Value;
            if (cacheKey != null)
                _parameterizedFactoryCache.TryRemove(cacheKey, out _);
        }
    }
}