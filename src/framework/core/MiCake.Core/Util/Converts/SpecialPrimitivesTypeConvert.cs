namespace MiCake.Core.Util.Converts
{
    internal class GuidValueConvert<TSource> : BaseConvert<TSource, Guid> where TSource : notnull
    {
        public override bool CanConvert(TSource value)
        {
            //Better scheme is needed for conversion, 
            //for example, when a parameter of type object is passed in (can be converted to guid)
            return typeof(TSource) == typeof(string) || value is Guid;
        }

        public override Guid Convert(TSource value)
        {
            Guid result = default;

            if (value is Guid guid)
            {
                return guid;
            }

            if (value is string strValue)
            {
                _ = Guid.TryParse(strValue, out result);
            }
            return result;
        }
    }

    internal class VersionValueConvert<TSource> : BaseConvert<TSource, Version> where TSource : notnull
    {
        public override bool CanConvert(TSource value)
        {
            return typeof(TSource) == typeof(string) || value is Version;
        }

        public override Version? Convert(TSource value)
        {
            Version? result = default;

            if (value is Version version)
            {
                return version;
            }

            if (value is string strValue)
            {
                _ = Version.TryParse(strValue, out result);
            }
            return result;
        }
    }

}
