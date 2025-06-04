using MiCake.Core.Util;
using System.Collections.Generic;

namespace MiCake.Core.Data
{
    public static class AccessorExtensions
    {
        public static T GetAccessor<T>(this IHasAccessor<T> accessor)
            => CheckValue.NotNull(accessor, nameof(accessor)).Instance;
    }
}
