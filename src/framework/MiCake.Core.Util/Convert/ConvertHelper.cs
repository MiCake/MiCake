using System;
using System.Collections.Generic;
using System.Text;

using static System.Convert;

namespace MiCake.Core.Util.Convert
{
    public static class ConvertHelper
    {
        /// <summary>
        /// Convert incoming type to destination type
        /// </summary>
        /// <typeparam name="TSource">source type</typeparam>
        /// <typeparam name="TDestination">destination type</typeparam>
        /// <param name="srouce">source value</param>
        /// <returns>if cannot convert,return defalut value.</returns>
        public static TDestination ConvertValue<TSource, TDestination>(TSource source)
        {
            try
            {
                TDestination result = default;

                result = (TDestination)ChangeType(source, typeof(TDestination));

                return result;
            }
            catch
            {
                return default(TDestination);
            }
        }
    }
}
