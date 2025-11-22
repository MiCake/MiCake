using System;

namespace MiCake.Util.Convert
{
    /// <summary>
    /// A specialized converter for converting values to the Guid type.
    /// Supports converting from string and other Guid values.
    /// </summary>
    /// <typeparam name="TSource">The source type to convert from.</typeparam>
    internal class GuidValueConverter<TSource> : SystemValueConverter<TSource, Guid> where TSource : notnull
    {
        /// <summary>
        /// Determines whether the given source value can be converted to Guid.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is a string or Guid; otherwise, false.</returns>
        public override bool CanConvert(TSource value)
        {
            return typeof(TSource) == typeof(string) || value is Guid;
        }

        /// <summary>
        /// Converts the given value to Guid.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted Guid, or empty Guid if conversion fails.</returns>
        public override Guid Convert(TSource value)
        {
            Guid result = default;

            if (value is Guid guidValue)
            {
                return guidValue;
            }

            if (value is string stringValue)
            {
                Guid.TryParse(stringValue, out result);
            }

            return result;
        }
    }
}
