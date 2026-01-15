using System;
using System.Collections.Generic;

namespace MiCake.Util.Convert
{
    /// <summary>
    /// Defines the interface for managing value converters.
    /// Provides methods to register, retrieve, and check for available converters.
    /// </summary>
    public interface IConverterRegistry
    {
        /// <summary>
        /// Registers a converter factory for converting from source type to destination type.
        /// The factory will be called to create converter instances when needed.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <param name="factory">A factory function that creates converter instances.</param>
        /// <remarks>
        /// If a converter for the same type pair is already registered,
        /// this method may replace it or add it to the registry (behavior depends on implementation).
        /// </remarks>
        void Register<TSource, TDestination>(
            Func<IValueConverter<TSource, TDestination>> factory)
            where TSource : notnull
            where TDestination : notnull;

        /// <summary>
        /// Registers a converter instance for converting from source type to destination type.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <param name="converter">The converter instance to register.</param>
        void Register<TSource, TDestination>(
            IValueConverter<TSource, TDestination> converter)
            where TSource : notnull
            where TDestination : notnull;

        /// <summary>
        /// Gets all registered converters for converting from source type to destination type.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <returns>An enumerable of converters for the specified type pair.</returns>
        IEnumerable<IValueConverter<TSource, TDestination>> GetConverters<TSource, TDestination>()
            where TSource : notnull
            where TDestination : notnull;

        /// <summary>
        /// Checks whether a converter is registered for converting from source type to destination type.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        /// <returns>True if at least one converter is registered for the type pair; otherwise, false.</returns>
        bool HasConverter<TSource, TDestination>()
            where TSource : notnull
            where TDestination : notnull;

        /// <summary>
        /// Clears all registered converters for the specified type pair.
        /// </summary>
        /// <typeparam name="TSource">The source type to convert from.</typeparam>
        /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
        void Clear<TSource, TDestination>()
            where TSource : notnull
            where TDestination : notnull;

        /// <summary>
        /// Clears all registered converters.
        /// </summary>
        void ClearAll();
    }
}
