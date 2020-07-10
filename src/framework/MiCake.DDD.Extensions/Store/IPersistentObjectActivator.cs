using MiCake.DDD.Extensions.Store.Mapping;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// Used to activate mapping between aggregateRoot and persistent object.
    /// </summary>
    public interface IPersistentObjectActivator
    {
        /// <summary>
        /// Activate mapping rules between aggregate roots and persistent objects.
        /// The premise is that you need to provide mapper through <see cref="SetMapper(IPersistentObjectMapper)"/>
        /// </summary>
        void ActivateMapping();

        /// <summary>
        /// Provider a <see cref="IPersistentObjectMapper"/> to config mapping rules.
        /// </summary>
        /// <param name="mapper"><see cref="IPersistentObjectMapper"/></param>
        void SetMapper(IPersistentObjectMapper mapper);
    }
}
