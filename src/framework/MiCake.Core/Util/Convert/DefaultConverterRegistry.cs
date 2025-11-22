using System;
using System.Collections.Generic;

namespace MiCake.Util.Convert
{
    /// <summary>
    /// Default implementation of IConverterRegistry.
    /// Manages registration and retrieval of value converters using a thread-safe registry.
    /// </summary>
    internal class DefaultConverterRegistry : IConverterRegistry
    {
        private readonly Dictionary<(Type, Type), List<object>> _converters = new();
        private readonly object _lock = new();

        /// <summary>
        /// Initializes the default registry with built-in converters for Guid and Version types.
        /// </summary>
        public DefaultConverterRegistry()
        {
            RegisterBuiltInConverters();
        }

        /// <summary>
        /// Registers a converter factory for the specified type pair.
        /// </summary>
        public void Register<TSource, TDestination>(
            Func<IValueConverter<TSource, TDestination>> factory)
            where TSource : notnull
            where TDestination : notnull
        {
            ArgumentNullException.ThrowIfNull(factory);

            lock (_lock)
            {
                var key = (typeof(TSource), typeof(TDestination));
                if (!_converters.TryGetValue(key, out List<object>? value))
                {
                    value = new List<object>();
                    _converters[key] = value;
                }

                value.Add(factory);
            }
        }

        /// <summary>
        /// Registers a converter instance for the specified type pair.
        /// </summary>
        public void Register<TSource, TDestination>(
            IValueConverter<TSource, TDestination> converter)
            where TSource : notnull
            where TDestination : notnull
        {
            ArgumentNullException.ThrowIfNull(converter);

            lock (_lock)
            {
                var key = (typeof(TSource), typeof(TDestination));
                if (!_converters.TryGetValue(key, out List<object>? value))
                {
                    value = new List<object>();
                    _converters[key] = value;
                }

                value.Add(converter);
            }
        }

        /// <summary>
        /// Gets all registered converters for the specified type pair.
        /// </summary>
        public IEnumerable<IValueConverter<TSource, TDestination>> GetConverters<TSource, TDestination>()
            where TSource : notnull
            where TDestination : notnull
        {
            lock (_lock)
            {
                var key = (typeof(TSource), typeof(TDestination));
                if (!_converters.TryGetValue(key, out List<object>? value))
                {
                    yield break;
                }

                foreach (var item in value)
                {
                    if (item is IValueConverter<TSource, TDestination> converter)
                    {
                        yield return converter;
                    }
                    else if (item is Func<IValueConverter<TSource, TDestination>> factory)
                    {
                        yield return factory();
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a converter is registered for the specified type pair.
        /// </summary>
        public bool HasConverter<TSource, TDestination>()
            where TSource : notnull
            where TDestination : notnull
        {
            lock (_lock)
            {
                var key = (typeof(TSource), typeof(TDestination));
                return _converters.ContainsKey(key) && _converters[key].Count > 0;
            }
        }

        /// <summary>
        /// Clears all converters for the specified type pair.
        /// </summary>
        public void Clear<TSource, TDestination>()
            where TSource : notnull
            where TDestination : notnull
        {
            lock (_lock)
            {
                var key = (typeof(TSource), typeof(TDestination));
                if (_converters.TryGetValue(key, out List<object>? value))
                {
                    value.Clear();
                }
            }
        }

        /// <summary>
        /// Clears all registered converters.
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _converters.Clear();
            }
        }

        /// <summary>
        /// Registers built-in converters for special types like Guid and Version.
        /// </summary>
        private void RegisterBuiltInConverters()
        {
            // Register Guid converters
            RegisterGuidConverters();

            // Register Version converters
            RegisterVersionConverters();
        }

        /// <summary>
        /// Registers Guid converters for various source types.
        /// </summary>
        private void RegisterGuidConverters()
        {
            RegisterGuidConverter<string>();
            RegisterGuidConverter<Guid>();
            RegisterGuidConverter<object>();
        }

        /// <summary>
        /// Helper method to register a Guid converter for a specific source type.
        /// </summary>
        private void RegisterGuidConverter<TSource>() where TSource : notnull
        {
            Register<TSource, Guid>(() => new GuidValueConverter<TSource>());
        }

        /// <summary>
        /// Registers Version converters for various source types.
        /// </summary>
        private void RegisterVersionConverters()
        {
            RegisterVersionConverter<string>();
            RegisterVersionConverter<Version>();
            RegisterVersionConverter<object>();
        }

        /// <summary>
        /// Helper method to register a Version converter for a specific source type.
        /// </summary>
        private void RegisterVersionConverter<TSource>() where TSource : notnull
        {
            Register<TSource, Version>(() => new VersionValueConverter<TSource>());
        }
    }
}
