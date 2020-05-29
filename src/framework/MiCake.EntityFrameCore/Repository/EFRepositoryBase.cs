using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// a base repository for efcore
    /// </summary>
    public abstract class EFRepositoryBase<TDbContext, TAggregateRoot, TKey>
         where TAggregateRoot : class, IAggregateRoot<TKey>
         where TDbContext : DbContext
    {
        protected virtual TDbContext DbContext
        {
            get
            {
                if (_currentDbContext != null)
                    return _currentDbContext;

                _currentDbContext = _dbContextProvider.GetDbContext();
                return _currentDbContext;
            }
        }
        protected virtual DbSet<TAggregateRoot> DbSet => DbContext.Set<TAggregateRoot>();

        private TDbContext _currentDbContext;
        private readonly IDbContextProvider<TDbContext> _dbContextProvider;

        public EFRepositoryBase(IDbContextProvider<TDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }
    }
}
