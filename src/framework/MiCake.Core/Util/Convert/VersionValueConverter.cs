using System;

namespace MiCake.Util.Convert
{
    /// <summary>
    /// A specialized converter for converting values to the Version type.
    /// Supports converting from string and other Version values.
    /// </summary>
    /// <typeparam name="TSource">The source type to convert from.</typeparam>
    internal class VersionValueConverter<TSource> : SystemValueConverter<TSource, Version> where TSource : notnull
    {
        /// <summary>
        /// Determines whether the given source value can be converted to Version.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is a string or Version; otherwise, false.</returns>
        public override bool CanConvert(TSource value)
        {
            return typeof(TSource) == typeof(string) || value is Version;
        }

        /// <summary>
        /// Converts the given value to Version.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted Version, or null if conversion fails.</returns>
        public override Version? Convert(TSource value)
        {
            Version? result = null;

            if (value is Version versionValue)
            {
                return versionValue;
            }

            if (value is string stringValue)
            {
                _ = Version.TryParse(stringValue, out result);
            }

            return result;
        }
    }
}
