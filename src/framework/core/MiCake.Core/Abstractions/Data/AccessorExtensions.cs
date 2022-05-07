using MiCake.Core.Util;

namespace MiCake.Core.Data
{
    /// <summary>
    ///     <para>
    ///         Extension methods for <see cref="IHasAccessor{TKey}" />.
    ///     </para>
    ///     <para>
    ///         <see cref="IHasAccessor{T}" /> is used to hide properties that are not intended to be used in
    ///         application code.
    ///     </para>
    /// </summary>
    public static class AccessorExtensions
    {
        public static T GetAccessor<T>(this IHasAccessor<T> accessor)
            => CheckValue.NotNull(accessor, nameof(accessor)).AccessibleData;
    }
}
