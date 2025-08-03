using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Extensions.Internal
{
    internal class DefaultRepositoryFacotry<TAggregateRoot, TKey> : IRepositoryFactory<TAggregateRoot, TKey>
         where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        private readonly List<AggregateRootDescriptor> _aggregateRootDescriptors;
        private readonly IRepositoryProvider<TAggregateRoot, TKey> _repositoryProvider;

        public DefaultRepositoryFacotry(
            IEnumerable<IRepositoryProvider<TAggregateRoot, TKey>> repositoryProvider,
            DomainMetadata domainMetadata
            )
        {
            //if has more repository provider(ef core,dapper,ado).always choose last one.
            _repositoryProvider = repositoryProvider.Last();

            _aggregateRootDescriptors = domainMetadata.DomainObject.AggregateRoots;
        }

        public IReadOnlyRepository<TAggregateRoot, TKey> CreateReadOnlyRepository()
        {
            CheckType(typeof(TAggregateRoot), typeof(TKey));

            return _repositoryProvider.GetReadOnlyRepository();
        }

        public IRepository<TAggregateRoot, TKey> CreateRepository()
        {
            CheckType(typeof(TAggregateRoot), typeof(TKey));

            return _repositoryProvider.GetRepository();
        }

        private void CheckType(Type aggregateType, Type keyType)
        {
            var aggregateDescriptor = _aggregateRootDescriptors.FirstOrDefault(s => s.Type == aggregateType) ??
                                        throw new NullReferenceException($"Cannot find {aggregateType.Name} metadata.Therefore, the IRepository cannot be created.");
        }
    }
}
