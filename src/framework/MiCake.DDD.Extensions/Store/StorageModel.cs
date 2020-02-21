using Mapster;
using MiCake.DDD.Domain;

namespace MiCake.DDD.Extensions.Store
{
    public interface IStorageModel
    {
        /// <summary>
        /// Configure relationship mapping between <see cref="IEntity"/> and storage model
        /// </summary>
        void ConfigureMapping();
    }

    /// <summary>
    /// Base class of storage model
    /// You should configure relationship mapping between <see cref="IEntity"/> and storage model by override <see cref="ConfigureMapping"/>
    /// </summary>
    /// <typeparam name="TEntity"><see cref="IEntity"/></typeparam>
    public abstract class StorageModel<TEntity> : IStorageModel
        where TEntity : IAggregateRoot
    {
        protected TypeAdapterConfig TypeConfig;

        /// <summary>
        /// Configure relationship mapping between <see cref="IEntity"/> and storage model.
        /// You can use <see cref="TypeConfig"/> property to config mapping.
        /// </summary>
        public abstract void ConfigureMapping();
    }
}
