using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions;
using MiCake.DDD.Extensions.Metadata;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.Uow;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.EntityFrameworkCore
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
            Check();

            IReadOnlyRepository<TAggregateRoot, TKey> result;

            var aggregateDescriptor = _aggregateRootsMetadata.First(s => s.Type.Equals(typeof(TAggregateRoot)));
            var uowManager = _serviceProvider.GetService<IUnitOfWorkManager>();

            if (aggregateDescriptor.HasPersistentObject && aggregateDescriptor.PersistentObject != null)
            {
                var type = typeof(EFReadOnlyRepositoryWithPO<,,,>).MakeGenericType(_options.DbContextType,
                                                                                 typeof(TAggregateRoot),
                                                                                 aggregateDescriptor.PersistentObject,
                                                                                 typeof(TKey));

                result = (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(type, uowManager);
            }
            else
            {
                var type = typeof(EFReadOnlyRepository<,,>).MakeGenericType(_options.DbContextType,
                                                                    typeof(TAggregateRoot),
                                                                    typeof(TKey));

                result = (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(type, uowManager);
            }

            return result;
        }

        public IRepository<TAggregateRoot, TKey> GetRepository()
        {
            Check();

            IRepository<TAggregateRoot, TKey> result;

            var aggregateDescriptor = _aggregateRootsMetadata.First(s => s.Type.Equals(typeof(TAggregateRoot)));
            var uowManager = _serviceProvider.GetService<IUnitOfWorkManager>();

            if (aggregateDescriptor.HasPersistentObject && aggregateDescriptor.PersistentObject != null)
            {
                var type = typeof(EFRepositoryWithPO<,,,>).MakeGenericType(_options.DbContextType,
                                                                                 typeof(TAggregateRoot),
                                                                                 aggregateDescriptor.PersistentObject,
                                                                                 typeof(TKey));

                result = (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(type, uowManager);
            }
            else
            {
                var type = typeof(EFRepository<,,>).MakeGenericType(_options.DbContextType,
                                                                    typeof(TAggregateRoot),
                                                                    typeof(TKey));

                result = (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(type, uowManager);
            }

            return result;
        }

        private void Check()
        {
            if (!_options.RegisterDefaultRepository)
                throw new ArgumentException($"the {nameof(_options.RegisterDefaultRepository)} value is false.so you can not use IReadOnlyRepository or IRepository");
        }
    }
}
