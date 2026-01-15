using System;
using System.Collections.Generic;

namespace MiCake.Util.Convert
{
    /// <summary>
    /// Provides a unified interface for converting values between types.
    /// Uses a registry-based approach to manage converters, supporting both built-in and custom converters.
    /// </summary>
    public static class ValueConverter
    {
        private static IConverterRegistry _registry = new DefaultConverterRegistry();

        /// <summary>
        /// Gets the current converter registry.
        /// </summary>
        public static IConverterRegistry Registry => _registry;

        /// <summary>
        /// Sets a custom converter registry.
        /// This allows users to provide their own registry implementation.
        /// </summary>
        /// <param name="registry">The custom registry to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when registry is null.</exception>
        public static void SetRegistry(IConverterRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Resets the registry to the default implementation.
        /// </summary>
        public static void ResetRegistry()
        {
            _registry = new DefaultConverterRegistry();
        }

        /// <summary>
        /// Converts the source value to the destination type.
        /// Attempts conversion using registered converters in order.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <param name="source">The source value to convert.</param>
        /// <returns>
        /// The converted value if successful, or the default value for the destination type if conversion fails.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
        public static TDestination? Convert<TSource, TDestination>(TSource source)
            where TSource : notnull
            where TDestination : notnull
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            try
            {
                // Try each registered converter in order
                // Note: Cannot use LINQ Where().Select().FirstOrDefault() pattern here because
                // it doesn't handle value types correctly (e.g., valid int 0 would be treated as conversion failure)
                foreach (var converter in _registry.GetConverters<TSource, TDestination>())
                {
                    if (!converter.CanConvert(source))
                        continue;

                    var result = converter.Convert(source);
                    if (!EqualityComparer<TDestination>.Default.Equals(result, default))
                    {
                        return result;
                    }
                }

                // Try system converter as fallback
                var systemConverter = new SystemValueConverter<TSource, TDestination>();
                if (systemConverter.CanConvert(source))
                {
                    return systemConverter.Convert(source);
                }

                return default;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Registers a converter factory for the specified type pair.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <param name="factory">A factory function that creates converter instances.</param>
        public static void RegisterConverter<TSource, TDestination>(
            Func<IValueConverter<TSource, TDestination>> factory)
            where TSource : notnull
            where TDestination : notnull
        {
            _registry.Register(factory);
        }

        /// <summary>
        /// Registers a converter instance for the specified type pair.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <param name="converter">The converter instance to register.</param>
        public static void RegisterConverter<TSource, TDestination>(
            IValueConverter<TSource, TDestination> converter)
            where TSource : notnull
            where TDestination : notnull
        {
            _registry.Register(converter);
        }

        /// <summary>
        /// Checks if a converter is registered for the specified type pair.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <returns>True if a converter is registered; otherwise, false.</returns>
        public static bool HasConverter<TSource, TDestination>()
            where TSource : notnull
            where TDestination : notnull
        {
            return _registry.HasConverter<TSource, TDestination>();
        }

        /// <summary>
        /// Clears all converters for the specified type pair.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        public static void ClearConverters<TSource, TDestination>()
            where TSource : notnull
            where TDestination : notnull
        {
            _registry.Clear<TSource, TDestination>();
        }

        /// <summary>
        /// Clears all registered converters.
        /// </summary>
        public static void ClearAll()
        {
            _registry.ClearAll();
        }
    }
}
