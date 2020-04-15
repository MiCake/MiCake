using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;

namespace Mapster
{
    public static class MapDomainEventExtension
    {
        /// <summary>
        /// Store aggregate domain Events to persistent object. 
        /// </summary>
        /// <typeparam name="TAggregateRoot"><see cref="IAggregateRoot"/></typeparam>
        /// <typeparam name="TPersistentObject"><see cref="IPersistentObject"/></typeparam>
        public static TypeAdapterSetter<TAggregateRoot, TPersistentObject> MapDomainEvent<TAggregateRoot, TPersistentObject>(this TypeAdapterSetter<TAggregateRoot, TPersistentObject> setter)
            where TAggregateRoot : IAggregateRoot
            where TPersistentObject : IPersistentObject
        {
            return setter.AfterMapping((s, d) => d.AddDomainEvents(s.GetDomainEvents()));
        }
    }
}
