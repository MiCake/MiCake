using MiCake.Cord.Modules;
using MiCake.EntityFrameworkCore.StorageInterpretor;
using MiCake.EntityFrameworkCore.StorageInterpretor.Strategy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.EntityFrameworkCore
{
    public static class EFCoreDbContextExtension
    {
        /// <summary>
        /// Add MiCake manage model for EFCore.
        /// If you don't inherit <see cref="MiCakeDbContext"/>, you can use this extension method in your DbContent OnModelCreating().
        /// </summary>
        /// <returns></returns>
        public static ModelBuilder AddMiCakeModel(this ModelBuilder builder, IServiceProvider currentScopeServices)
        {
            var domainStoreConfig = DDDModuleHelper.MustGetStoreConfig(currentScopeServices);

            //EFCore will cached model info.This method only called once
            EFInterpretorOptions efCoreExpressionOptions = new();
            efCoreExpressionOptions.AddCoreStrategies();    // add core config strategy.

            var efCoreStoreExpression = new DefaultEFStoreModelInterpretor(efCoreExpressionOptions);
            efCoreStoreExpression.Interpret(domainStoreConfig.GetStoreModel(), builder);

            return builder;
        }

        /// <summary>
        /// Add MiCake configure for EFCore.(include repository lifetime etc.)
        /// If you don't inherit <see cref="MiCakeDbContext"/>, you can use this extension method in your DbContent OnConfiguring().
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="currentScopeServices"></param>
        /// <returns></returns>
        public static DbContextOptionsBuilder AddMiCakeConfigure(this DbContextOptionsBuilder optionsBuilder, IServiceProvider currentScopeServices)
        {
            return optionsBuilder;
        }

        public static async Task<int> AddMiCakeSaveChangeHandler(this Func<Task<int>> baseSaveChangeFunc, IServiceProvider currentScopeServices, DbContext dbContext, CancellationToken cancellationToken = default)
        {
            var efSaveChangesLifetime = currentScopeServices.GetService<IEFSaveChangesLifetime>();

            IEnumerable<EntityEntry> changedEntites = efSaveChangesLifetime == null ? new List<EntityEntry>() : GetChangeEntities(dbContext);
            if (efSaveChangesLifetime != null)
                await efSaveChangesLifetime.BeforeSaveChangesAsync(changedEntites, cancellationToken);

            var result = await baseSaveChangeFunc();

            if (efSaveChangesLifetime != null)
                await efSaveChangesLifetime.AfterSaveChangesAsync(changedEntites, cancellationToken);

            return result;
        }

        private static IEnumerable<EntityEntry> GetChangeEntities(DbContext dbContext)
        {
            return dbContext.ChangeTracker.Entries();   // ChangeTracker.Entries() and ChangeTracker.Entries<TEntity>() will trigger ChangeTracker.DetectChanges();
        }
    }
}
