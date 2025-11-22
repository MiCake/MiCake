using System;
using MiCake.Util.Cache;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MiCake.Util.Reflection
{
    /// <summary>
    /// Provides high-performance object instantiation using compiled expressions.
    /// Much faster than Activator.CreateInstance for repeated instantiations.
    /// </summary>
    public static class CompiledActivator
    {
        private const int MaxParameterizedCacheSize = 256;

        // Use a bounded LRU cache to avoid unbounded memory growth (potential leak)
        private const int MaxFactoryCacheSize = 1000;
        private static readonly BoundedLruCache<Type, Func<object>> _factoryCache = new(MaxFactoryCacheSize);
        // parameterized factories cache - bounded using a LRU cache keyed by (Type, Type[])
        private static readonly BoundedLruCache<ParameterizedFactoryKey, CacheEntry> _parameterizedFactoryCache = new(MaxParameterizedCacheSize);

        private class CacheEntry
        {
            public Func<object[], object> Factory { get; set; } = null!;
        }

        // Lightweight value type key describes a parameterized factory signature
        internal readonly struct ParameterizedFactoryKey : IEquatable<ParameterizedFactoryKey>
        {
            public readonly Type Type;
            public readonly Type[] ArgTypes;

            public ParameterizedFactoryKey(Type type, Type[] argTypes)
            {
                Type = type ?? throw new ArgumentNullException(nameof(type));
                ArgTypes = argTypes ?? Array.Empty<Type>();
            }

            public bool Equals(ParameterizedFactoryKey other)
            {
                if (!ReferenceEquals(Type, other.Type))
                    return false;

                if (ArgTypes.Length != other.ArgTypes.Length)
                    return false;

                for (int i = 0; i < ArgTypes.Length; i++)
                {
                    if (!ReferenceEquals(ArgTypes[i], other.ArgTypes[i]))
                        return false;
                }

                return true;
            }

            public override bool Equals(object? obj) => obj is ParameterizedFactoryKey k && Equals(k);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = Type.GetHashCode();
                    for (int i = 0; i < ArgTypes.Length; i++)
                    {
                        hash = (hash * 31) + (ArgTypes[i]?.GetHashCode() ?? 0);
                    }
                    return hash;
                }
            }
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

            var factory = GetOrAddParameterizedFactory(type, argTypes);
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

        private static Func<object[], object> GetOrAddParameterizedFactory(Type type, Type[] parameterTypes)
        {
            var key = new ParameterizedFactoryKey(type, parameterTypes);
            var cached = _parameterizedFactoryCache.GetOrAdd(key, k => new CacheEntry { Factory = CreateParameterizedFactory(k.Type, k.ArgTypes) });

            return cached.Factory;
        }
    }
}