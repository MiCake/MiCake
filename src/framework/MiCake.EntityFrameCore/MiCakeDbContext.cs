using MiCake.Core.DependencyInjection;
using MiCake.DDD.Extensions.Store.Configure;
using MiCake.EntityFrameworkCore.Interprets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class MiCakeDbContext : DbContext
    {
        private IServiceScope currentSavechangeLifetimeScope;
        private IEFSaveChangesLifetime _saveChangesLifetime;
        protected IEFSaveChangesLifetime SaveChangesLifetime
        {
            get
            {
                if (_saveChangesLifetime != null)
                    return _saveChangesLifetime;

                currentSavechangeLifetimeScope = ServiceLocator.Instance.GetSerivce<IServiceScopeFactory>().CreateScope();
                _saveChangesLifetime = currentSavechangeLifetimeScope.ServiceProvider.GetService<IEFSaveChangesLifetime>() ??
                    throw new ArgumentNullException($"Can not reslove {nameof(IEFSaveChangesLifetime)},Please check that this service is registered with DI.");

                return _saveChangesLifetime;
            }
        }

        public MiCakeDbContext(DbContextOptions options) : base(options)
        {
        }

        protected MiCakeDbContext() : base()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            currentSavechangeLifetimeScope?.Dispose();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="modelBuilder"><see cref="ModelBuilder"/></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //EFCore will cached model info.This method only called once
            var storeModelExpression = new EFModelExpressionProvider().GetExpression();
            storeModelExpression.Interpret(StoreConfig.Instance.GetStoreModel(), modelBuilder);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var entities = ChangeTracker.Entries();

            SaveChangesLifetime.BeforeSaveChanges(entities);
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            SaveChangesLifetime.AfterSaveChanges(entities);

            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var entities = ChangeTracker.Entries();

            await SaveChangesLifetime.BeforeSaveChangesAsync(entities, cancellationToken);
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            await SaveChangesLifetime.AfterSaveChangesAsync(entities, cancellationToken);

            return result;
        }
    }
}
