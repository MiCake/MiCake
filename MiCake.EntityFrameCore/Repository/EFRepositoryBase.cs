using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Uow;
using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// a base repository for efcore
    /// </summary>
    public abstract class EFRepositoryBase<TDbContext, TAggregateRoot, TKey>
         where TAggregateRoot : class, IAggregateRoot<TKey>
         where TDbContext : DbContext
    {
        protected virtual TDbContext DbContext => _dbContextFactory.CreateDbContext();
        protected virtual DbSet<TAggregateRoot> DbSet => DbContext.Set<TAggregateRoot>();

        private readonly IUnitOfWorkManager _uowManager;
        private IUowDbContextFactory<TDbContext> _dbContextFactory;

        public EFRepositoryBase(IUnitOfWorkManager uowManager)
        {
            _uowManager = uowManager;

            _dbContextFactory = new UowDbContextFactory<TDbContext>(_uowManager);
        }
    }
}
