using Microsoft.EntityFrameworkCore;
using System;

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
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddMiCakeModel();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.AddMiCakeConfigure(CurrentScopeServices);
        }
    }
}
