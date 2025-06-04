using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Uow
{
    public class DbContextProvider<TDbContext> : IDbContextProvider<TDbContext>
         where TDbContext : DbContext
    {
        private readonly ICurrentUnitOfWork _currentUnitOfWork;
        private readonly IObjectAccessor<MiCakeEFCoreOptions> optAccessor;

        public DbContextProvider(ICurrentUnitOfWork currentUnitOfWork, IObjectAccessor<MiCakeEFCoreOptions> opts)
        {
            _currentUnitOfWork = currentUnitOfWork;
            optAccessor = opts;
        }

        public async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
        {
            if (_currentUnitOfWork?.Value == null)
                throw new ArgumentNullException($"There has no any unit of work in this service scoped!");

            var uow = _currentUnitOfWork.Value;
            var dbContext = uow.ServiceScope.ServiceProvider.GetService<TDbContext>()
                                ?? throw new ArgumentNullException($"Can not resolve a {typeof(TDbContext).FullName} form this serve scoped,please check has added dbcontext in DI!");

            //Add this dbContext to current uow.
            await uow.TryAddDbExecutorAsync(CreateDbContextExecutor(dbContext), cancellationToken);

            return dbContext;
        }

        IDbExecutor CreateDbContextExecutor(TDbContext dbContext)
        {
            var dbExecutorType = typeof(DbContextExecutor<>).MakeGenericType(typeof(TDbContext));
            return (IDbExecutor)Activator.CreateInstance(dbExecutorType, dbContext, optAccessor);
        }

        async Task<DbContext> IDbContextProvider.GetDbContextAsync(CancellationToken cancellationToken)
        {
            return await GetDbContextAsync(cancellationToken);
        }
    }
}