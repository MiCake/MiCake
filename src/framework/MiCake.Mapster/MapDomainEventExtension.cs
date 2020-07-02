using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;

namespace Mapster
{
    public static class MapDomainEventExtension
    {
        /// <summary>
        /// Store aggregate domain Events to persistent object. 
        /// </summary>
        /// <typeparam name="TEntity"><see cref="IAggregateRoot"/></typeparam>
        /// <typeparam name="TPersistentObject"><see cref="IPersistentObject"/></typeparam>
        public static TypeAdapterSetter<TEntity, TPersistentObject> MapDomainEvent<TEntity, TPersistentObject>(this TypeAdapterSetter<TEntity, TPersistentObject> setter)
            where TEntity : IEntity
            where TPersistentObject : IPersistentObject
        {
            return setter.AfterMapping((s, d) => d.AddDomainEvents(s.GetDomainEvents()));
        }
    }
}
