using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Store;

namespace Mapster
{
    public static class MapDomainEventExtension
    {
        /// <summary>
        /// Store aggregate domain Events to storage model 
        /// </summary>
        /// <typeparam name="TAggregateRoot"><see cref="IAggregateRoot"/></typeparam>
        /// <typeparam name="TStorageModel"><see cref="IStorageModel"/></typeparam>
        public static TypeAdapterSetter<TAggregateRoot, TStorageModel> MapDomainEvent<TAggregateRoot, TStorageModel>(this TypeAdapterSetter<TAggregateRoot, TStorageModel> setter)
            where TAggregateRoot : IAggregateRoot
            where TStorageModel : IStorageModel
        {
            return setter.MapToTargetWith((s, d) => (TStorageModel)d.AddDomainEvents(s.GetDomainEvents()));
        }
    }
}
