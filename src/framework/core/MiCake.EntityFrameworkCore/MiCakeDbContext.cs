using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class MiCakeDbContext : DbContext
    {
        public IServiceProvider CurrentScopeServices { get; }

        public MiCakeDbContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
        {
            CurrentScopeServices = serviceProvider;
        }

        protected MiCakeDbContext() : base()
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            CheckCurrentServiceProvider();

            base.OnModelCreating(modelBuilder);
            modelBuilder.AddMiCakeModel(CurrentScopeServices);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            CheckCurrentServiceProvider();

            base.OnConfiguring(optionsBuilder);
            optionsBuilder.AddMiCakeConfigure(CurrentScopeServices);
        }

        public override int SaveChanges()
        {
            return SaveChanges(true);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            CheckCurrentServiceProvider();

            return EFCoreDbContextExtension.AddMiCakeSaveChangeHandler(() => Task.FromResult(base.SaveChanges(acceptAllChangesOnSuccess)), CurrentScopeServices, this).Result;
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            CheckCurrentServiceProvider();

            return EFCoreDbContextExtension.AddMiCakeSaveChangeHandler(() => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken), CurrentScopeServices, this, cancellationToken);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return SaveChangesAsync(true, cancellationToken);
        }

        protected void CheckCurrentServiceProvider()
        {
            if (CurrentScopeServices == null)
            {
                throw new InvalidOperationException($"Sorry, you can only get DbContext by dependency injection instead of by new().");
            }
        }
    }
}
