namespace MiCake.Util.Convert
{
    /// <summary>
    /// A base converter that uses System.Convert.ChangeType to handle general type conversions.
    /// This converter supports converting between most primitive types (int, string, DateTime, etc.).
    /// </summary>
    /// <typeparam name="TSource">The source type to convert from.</typeparam>
    /// <typeparam name="TDestination">The destination type to convert to.</typeparam>
    internal class SystemValueConverter<TSource, TDestination> : IValueConverter<TSource, TDestination>
        where TDestination : notnull
        where TSource : notnull
    {
        /// <summary>
        /// Determines whether the given source value can be converted.
        /// The base converter accepts all values.
        /// </summary>
        public virtual bool CanConvert(TSource value)
        {
            return true;
        }

        /// <summary>
        /// Converts the source value using System.Convert.ChangeType.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value, or null if conversion fails.</returns>
        public virtual TDestination? Convert(TSource value)
        {
            try
            {
                return (TDestination)System.Convert.ChangeType(value, typeof(TDestination));
            }
            catch
            {
                return default;
            }
        }
    }
}
