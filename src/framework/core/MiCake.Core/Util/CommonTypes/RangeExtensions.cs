namespace MiCake.Core.Util
{
    public static class RangeExtensions
    {
        /// <summary>
        /// Judge whether a value is within the range
        /// </summary>
        /// <param name="range">The given range</param>
        /// <param name="value">Comparison value</param>
        public static bool IsInRange(this Range range, int value)
            => range.Start.Value <= value && range.End.Value >= value;
    }
}
