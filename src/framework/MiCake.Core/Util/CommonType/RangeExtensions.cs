using System;

namespace MiCake.Util
{
    public static class RangeExtensions
    {
        /// <summary>
        /// Checks if a given value is within the specified range.
        /// </summary>
        /// <param name="range">The given range</param>
        /// <param name="value">Comparison value</param>
        public static bool IsInRange(this Range range, int value)
            => range.Start.Value <= value && range.End.Value >= value;
    }
}
