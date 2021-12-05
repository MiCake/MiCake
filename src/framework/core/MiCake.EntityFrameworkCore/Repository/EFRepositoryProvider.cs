using MiCake.Core.DependencyInjection;
using MiCake.DDD.Connector;
using MiCake.DDD.Domain;
using System;
using System.Collections.Concurrent;

namespace MiCake.EntityFrameworkCore.Repository
{
    internal class EFRepositoryProvider<TAggregateRoot, TKey> : IRepositoryProvider<TAggregateRoot, TKey>
         where TAggregateRoot : class, IAggregateRoot<TKey>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MiCakeEFCoreOptions _options;

        static readonly ConcurrentDictionary<Type, Type> _repoTypeCache = new();
        static readonly ConcurrentDictionary<Type, Type> _readOnlyRepoTypeCache = new();

        public EFRepositoryProvider(
            IServiceProvider serviceProvider,
            IObjectAccessor<MiCakeEFCoreOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        public IReadOnlyRepository<TAggregateRoot, TKey> GetReadOnlyRepository()
        {
            var repoType = _readOnlyRepoTypeCache.GetOrAdd(typeof(TAggregateRoot),
                  key => typeof(EFReadOnlyRepository<,,>).MakeGenericType(_options.DbContextType, typeof(TAggregateRoot), typeof(TKey)));

            return (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(repoType, _serviceProvider);
        }

        public IRepository<TAggregateRoot, TKey> GetRepository()
        {
            var repoType = _repoTypeCache.GetOrAdd(typeof(TAggregateRoot),
                  key => typeof(EFRepository<,,>).MakeGenericType(_options.DbContextType, typeof(TAggregateRoot), typeof(TKey)));

            return (IRepository<TAggregateRoot, TKey>)Activator.CreateInstance(repoType, _serviceProvider);
        }
    }
}
