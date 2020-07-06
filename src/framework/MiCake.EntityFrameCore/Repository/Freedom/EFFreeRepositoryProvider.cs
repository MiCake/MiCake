using MiCake.Core.DependencyInjection;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using MiCake.DDD.Extensions;
using System;

namespace MiCake.EntityFrameworkCore.Repository.Freedom
{
    class EFFreeRepositoryProvider<TEntity, TKey> : IFreeRepositoryProvider<TEntity, TKey>
         where TEntity : class, IEntity<TKey>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MiCakeEFCoreOptions _options;

        public EFFreeRepositoryProvider(
            IServiceProvider serviceProvider,
            IObjectAccessor<MiCakeEFCoreOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options.Value;
        }

        public IFreeRepository<TEntity, TKey> GetFreeRepository()
        {
            var type = typeof(EFReadOnlyFreeRepository<,,>).MakeGenericType(_options.DbContextType, typeof(TEntity), typeof(TKey));
            return (IFreeRepository<TEntity, TKey>)Activator.CreateInstance(type, _serviceProvider);
        }

        public IReadOnlyFreeRepository<TEntity, TKey> GetReadOnlyFreeRepository()
        {
            var type = typeof(EFFreeRepository<,,>).MakeGenericType(_options.DbContextType, typeof(TEntity), typeof(TKey));
            return (IReadOnlyFreeRepository<TEntity, TKey>)Activator.CreateInstance(type, _serviceProvider);
        }
    }
}
