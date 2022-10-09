namespace MiCake.Core.Util.Converts
{
    /// <summary>
    /// Defined a class can convert incoming type to destination type.
    /// </summary>
    /// <typeparam name="TSource">source type</typeparam>
    /// <typeparam name="TDestination">destination type</typeparam>
    public interface IValueConvert<in TSource, out TDestination> where TSource : notnull where TDestination : notnull
    {
        /// <summary>
        /// Convert value to destination type.
        /// </summary>
        /// <param name="value">source value.</param>
        TDestination? Convert(TSource value);

        /// <summary>
        /// Indicates whether the current incoming value can be converted
        /// </summary>
        /// <param name="value">source value.</param>
        bool CanConvert(TSource value);
    }
}
