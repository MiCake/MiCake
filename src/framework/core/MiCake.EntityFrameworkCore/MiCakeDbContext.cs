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

#pragma warning disable CS8618
        protected MiCakeDbContext() : base()
#pragma warning restore CS8618
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
            CheckCurrentServiceProvider();

            return EFCoreDbContextExtension.AddMiCakeSaveChangeHandler(() => Task.FromResult(base.SaveChanges()), CurrentScopeServices, this).Result;
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
            CheckCurrentServiceProvider();

            return EFCoreDbContextExtension.AddMiCakeSaveChangeHandler(() => base.SaveChangesAsync(cancellationToken), CurrentScopeServices, this, cancellationToken);
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
