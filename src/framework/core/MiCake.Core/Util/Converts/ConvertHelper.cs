namespace MiCake.Core.Util.Converts
{
    public static class ConvertHelper
    {
        /// <summary>
        /// Convert incoming type to destination type
        /// </summary>
        /// <typeparam name="TSource">source type</typeparam>
        /// <typeparam name="TDestination">destination type</typeparam>
        /// <param name="source">source value</param>
        /// <returns>if cannot convert,return defalut value.</returns>
        public static TDestination? ConvertValue<TSource, TDestination>(TSource source) where TSource : notnull where TDestination : notnull
        {
            try
            {
                TDestination? result = default;

                foreach (var converter in GetConverters<TSource, TDestination>())
                {
                    if (converter.CanConvert(source))
                    {
                        result = converter.Convert(source);
                        break;
                    }
                }
                return result;
            }
            catch
            {
                return default;
            }
        }

        private static IEnumerable<IValueConvert<TSource, TDestination>> GetConverters<TSource, TDestination>() where TSource : notnull where TDestination : notnull
        {
            if (typeof(TDestination) == typeof(Guid))
            {
                yield return (IValueConvert<TSource, TDestination>)new GuidValueConvert<TSource>();
            }
            if (typeof(TDestination) == typeof(Version))
            {
                yield return (IValueConvert<TSource, TDestination>)new VersionValueConvert<TSource>();
            }

            yield return new BaseConvert<TSource, TDestination>();
        }
    }
}
