using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using MiCake.DDD.Extensions;
using System;
using System.Collections.Concurrent;

namespace MiCake.EntityFrameworkCore.Repository.Freedom
{
    class EFFreeRepositoryProvider<TEntity, TKey> : IFreeRepositoryProvider<TEntity, TKey>
         where TEntity : class, IEntity<TKey>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MiCakeEFCoreOptions _options;

        static readonly ConcurrentDictionary<Type, Type> _repoTypeCache = new();
        static readonly ConcurrentDictionary<Type, Type> _readOnlyRepoTypeCache = new();

        public EFFreeRepositoryProvider(
            IServiceProvider serviceProvider,
            IObjectAccessor<MiCakeEFCoreOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        public IFreeRepository<TEntity, TKey> GetFreeRepository()
        {
            var type = _repoTypeCache.GetOrAdd(typeof(TEntity), key => typeof(EFReadOnlyFreeRepository<,,>).MakeGenericType(_options.DbContextType, typeof(TEntity), typeof(TKey)));
            return (IFreeRepository<TEntity, TKey>)Activator.CreateInstance(type, _serviceProvider);
        }

        public IReadOnlyFreeRepository<TEntity, TKey> GetReadOnlyFreeRepository()
        {
            var type = _readOnlyRepoTypeCache.GetOrAdd(typeof(TEntity), typeof(EFFreeRepository<,,>).MakeGenericType(_options.DbContextType, typeof(TEntity), typeof(TKey)));
            return (IReadOnlyFreeRepository<TEntity, TKey>)Activator.CreateInstance(type, _serviceProvider);
        }
    }
}
