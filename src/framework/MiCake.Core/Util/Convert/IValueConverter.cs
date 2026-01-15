namespace MiCake.Util.Convert
{
    /// <summary>
    /// Defines the interface for value conversion from source type to destination type.
    /// Implementations should support converting between types and provide validation capability.
    /// </summary>
    /// <typeparam name="TSource">The source type to convert from.</typeparam>
    /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
    public interface IValueConverter<in TSource, TDestination> 
        where TDestination : notnull 
        where TSource : notnull
    {
        /// <summary>
        /// Determines whether the given source value can be converted to the destination type.
        /// </summary>
        /// <param name="value">The source value to check for conversion capability.</param>
        /// <returns>True if the value can be converted; otherwise, false.</returns>
        bool CanConvert(TSource value);

        /// <summary>
        /// Converts the given source value to the destination type.
        /// </summary>
        /// <param name="value">The source value to convert.</param>
        /// <returns>The converted value, or null if conversion fails.</returns>
        TDestination? Convert(TSource value);
    }
}
