namespace MiCake.Core.Modularity
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RelyOnAttribute : Attribute
    {
        /// <summary>
        /// Other <see cref="MiCakeModule"/> type that this module depends on
        /// </summary>
        public Type[] RelyOnTypes { get; }

        public RelyOnAttribute(params Type[] relyOnTypes)
        {
            RelyOnTypes = relyOnTypes ?? new Type[0];
        }

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
