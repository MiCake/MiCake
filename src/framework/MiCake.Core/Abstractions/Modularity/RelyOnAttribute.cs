using System;

namespace MiCake.Core.Modularity
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RelyOnAttribute(params Type[] relyOnTypes) : Attribute
    {
        /// <summary>
        /// Other <see cref="MiCakeModule"/> type that this module depends on
        /// </summary>
        public Type[] RelyOnTypes { get; } = relyOnTypes ?? [];

        /// <summary>
        /// Get dependent modules
        /// </summary>
        /// <returns></returns>
        public virtual Type[] GetRelyOnTypes()
        {
            return RelyOnTypes;
        }
    }
}
