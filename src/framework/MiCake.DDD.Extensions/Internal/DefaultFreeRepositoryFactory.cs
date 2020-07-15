using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using MiCake.DDD.Extensions.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Extensions.Internal
{
    internal class DefaultFreeRepositoryFactory<TEntity, TKey> : IFreeRepositoryFactory<TEntity, TKey>
          where TEntity : class, IEntity<TKey>
    {
        private readonly List<EntityDescriptor> _entityDescriptors;
        private readonly IFreeRepositoryProvider<TEntity, TKey> _repositoryProvider;

        public DefaultFreeRepositoryFactory(
            IEnumerable<IFreeRepositoryProvider<TEntity, TKey>> repositoryProvider,
            DomainMetadata domainMetadata
            )
        {
            //if has more repository provider(ef core,dapper,ado).always choose last one.
            _repositoryProvider = repositoryProvider.Last();

            _entityDescriptors = domainMetadata.DomainObject.Entities;
        }

        public IFreeRepository<TEntity, TKey> CreateFreeRepository()
        {
            CheckType(typeof(TEntity), typeof(TKey));

            return _repositoryProvider.GetFreeRepository();
        }

        public IReadOnlyFreeRepository<TEntity, TKey> CreateReadOnlyFreeRepository()
        {
            CheckType(typeof(TEntity), typeof(TKey));

            return _repositoryProvider.GetReadOnlyFreeRepository();
        }

        private void CheckType(Type aggregateType, Type keyType)
        {
            var entityDescriptor = _entityDescriptors.FirstOrDefault(s => s.Type == aggregateType) ??
                                        throw new NullReferenceException($"Cannot find {aggregateType.Name} metadata.Therefore, the IRepository cannot be created.");
        }
    }
}
