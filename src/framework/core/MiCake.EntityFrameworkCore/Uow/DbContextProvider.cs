using MiCake.Uow;
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

        public DbContextProvider(ICurrentUnitOfWork currentUnitOfWork)
        {
            _currentUnitOfWork = currentUnitOfWork;
        }

        public async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
        {
            if (_currentUnitOfWork?.Value == null)
                throw new ArgumentNullException($"There has no any unit of work in this service scoped!");

            var uow = _currentUnitOfWork.Value;
            var dbContext = uow.ServiceScope.ServiceProvider.GetService<TDbContext>();

            if (dbContext == null)
                throw new ArgumentNullException($"Can not resolve a {typeof(TDbContext).FullName} form this serve scoped,please check has added dbcontext in DI!");

            //Add this dbContext to current uow.
            await uow.TryAddDbExecutorAsync(CreateDbContextExecutor(dbContext));

            return dbContext;
        }

        IDbExecutor CreateDbContextExecutor(TDbContext dbContext)
        {
            var dbExecutorType = typeof(DbContextExecutor<>).MakeGenericType(typeof(TDbContext));
            return (IDbExecutor)Activator.CreateInstance(dbExecutorType, dbContext);
        }

        async Task<DbContext> IDbContextProvider.GetDbContextAsync(CancellationToken cancellationToken)
        {
            return await GetDbContextAsync(cancellationToken);
        }
    }
}
