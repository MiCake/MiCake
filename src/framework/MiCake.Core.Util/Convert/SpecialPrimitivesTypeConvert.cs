using System;

namespace MiCake.Core.Util.Convert
{
    internal class GuidValueConvert<TSource> : BaseConvert<TSource, Guid>
    {
        public override bool CanConvert(TSource value)
        {
            //Better scheme is needed for conversion, 
            //for example, when a parameter of type object is passed in (can be converted to guid)
            return typeof(TSource) == typeof(string);
        }

        public override Guid Convert(TSource value)
        {
            Guid result = default;
            if (value is string strValue)
            {
                Guid.TryParse(strValue, out result);
            }
            return result;
        }
    }
}
