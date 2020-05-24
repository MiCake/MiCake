using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

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

        public TDbContext GetDbContext()
        {
            if (_currentUnitOfWork?.Value == null)
                throw new ArgumentNullException($"There has no any unit of work in this service scoped!");

            var uow = _currentUnitOfWork.Value;
            var dbContext = uow.ServiceScope.ServiceProvider.GetService<TDbContext>();

            if (dbContext == null)
                throw new ArgumentNullException($"Can not resolve a {typeof(TDbContext).FullName} form this serve scoped,please check has added dbcontext in DI!");

            //询问uow的transactionprovider是否需要开启一个事务，如果是则把该事务给予DbContext

            return dbContext;
        }

        DbContext IDbContextProvider.GetDbContext()
            => GetDbContext();
    }
}
