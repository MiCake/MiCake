using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// a base repository for EFCore
    /// </summary>
    public abstract class EFRepositoryBase<TDbContext, TEntity, TKey>
         where TEntity : class, IEntity<TKey>
         where TDbContext : DbContext
    {
        private readonly object lockObj = new object();

        /// <summary>
        /// Use to get need services.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        private TDbContext? _dbContext;
        protected TDbContext? CurrentDbContext
        {
            get
            {
                if (_dbContext != null)
                {
                    return _dbContext;
                }
                else
                {
                    lock (lockObj)
                    {
                        _dbContext = GetDbContext();

                        return _dbContext;
                    }
                }
            }
        }

        protected DbSet<TEntity> DbSet => CurrentDbContext!.Set<TEntity>();

        public EFRepositoryBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        private TDbContext GetDbContext()
        {
            var currentTransactionObjs = ServiceProvider.GetService<IUnitOfWorkManager>()?.GetCurrentUnitOfWork()?.GetTransactionObjects();
            if (currentTransactionObjs == null || currentTransactionObjs.Count == 0)
            {
                // if there is no uow,give dbcontext from service provider directly.
                return ServiceProvider.GetService<TDbContext>()!;
            }

            var currentEfCoreDbContext = currentTransactionObjs.Where(s => s is EFCoreTransactionObject).FirstOrDefault();
            if (currentTransactionObjs == null)
            {
                throw new InvalidOperationException($"Get {currentTransactionObjs!.Count} transaction object,but there have no {nameof(EFCoreTransactionObject)}.");
            }

            return (TDbContext)currentEfCoreDbContext!.TransactionInstance;
        }
    }
}
