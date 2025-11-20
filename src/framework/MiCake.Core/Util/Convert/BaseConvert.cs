using static System.Convert;

namespace MiCake.Util.Convert
{
    internal class BaseConvert<TSource, TDestination> : IValueConvert<TSource, TDestination>
    {
        public virtual bool CanConvert(TSource value)
        {
            return true;
        }

        public virtual TDestination Convert(TSource value)
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
