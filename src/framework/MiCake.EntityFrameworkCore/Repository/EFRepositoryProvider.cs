using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.EntityFrameworkCore.Repository
{
    internal class EFRepositoryProvider<TAggregateRoot, TKey> : IRepositoryProvider<TAggregateRoot, TKey>
         where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<AggregateRootDescriptor> _aggregateRootsMetadata;
        private readonly MiCakeEFCoreOptions _options;

        public EFRepositoryProvider(
            IServiceProvider serviceProvider,
            DomainMetadata domainMetadata,
            IObjectAccessor<MiCakeEFCoreOptions> options)
        {
            _serviceProvider = serviceProvider;
            _aggregateRootsMetadata = domainMetadata.DomainObject.AggregateRoots;
            _options = options.Value;
        }

        public IReadOnlyRepository<TAggregateRoot, TKey> GetReadOnlyRepository()
        {
            IReadOnlyRepository<TAggregateRoot, TKey> result;

            var aggregateDescriptor = _aggregateRootsMetadata.First(s => s.Type.Equals(typeof(TAggregateRoot)));

            if (aggregateDescriptor.HasPersistentObject && aggregateDescriptor.PersistentObject != null)
            {
                var type = typeof(EFReadOnlyRepositoryWithPO<,,,>).MakeGenericType(_options.DbContextType,
                                                                                 typeof(TAggregateRoot),
                                                                                 aggregateDescriptor.PersistentObject,
                                                                                 typeof(TKey));

                result = (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(type, _serviceProvider);
            }
            else
            {
                var type = typeof(EFReadOnlyRepository<,,>).MakeGenericType(_options.DbContextType,
                                                                    typeof(TAggregateRoot),
                                                                    typeof(TKey));

                result = (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(type, _serviceProvider);
            }

            return result;
        }

        public IRepository<TAggregateRoot, TKey> GetRepository()
        {
            IRepository<TAggregateRoot, TKey> result;

            var aggregateDescriptor = _aggregateRootsMetadata.First(s => s.Type.Equals(typeof(TAggregateRoot)));

            if (aggregateDescriptor.HasPersistentObject && aggregateDescriptor.PersistentObject != null)
            {
                var type = typeof(EFRepositoryWithPO<,,,>).MakeGenericType(_options.DbContextType,
                                                                                 typeof(TAggregateRoot),
                                                                                 aggregateDescriptor.PersistentObject,
                                                                                 typeof(TKey));

                result = (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(type, _serviceProvider);
            }
            else
            {
                var type = typeof(EFRepository<,,>).MakeGenericType(_options.DbContextType,
                                                                    typeof(TAggregateRoot),
                                                                    typeof(TKey));

                result = (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(type, _serviceProvider);
            }

            return result;
        }
    }
}
