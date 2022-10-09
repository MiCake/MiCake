using static System.Convert;

namespace MiCake.Core.Util.Converts
{
    internal class BaseConvert<TSource, TDestination> : IValueConvert<TSource, TDestination> where TSource : notnull where TDestination : notnull
    {
        public virtual bool CanConvert(TSource value)
        {
            return true;
        }

        public virtual TDestination? Convert(TSource value)
        {
            try
            {
                return (TDestination)ChangeType(value, typeof(TDestination));
            }
            catch
            {
                return default;
            }
        }
    }
}
