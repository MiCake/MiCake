using Microsoft.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Extensions;
using MiCake.EntityFrameworkCore.Internal;
using System;
using System.Linq;

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
            
            // Apply simplified MiCake conventions
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Select(t => t.ClrType)
                .Where(t => t != null)
                .ToArray();
                
            modelBuilder.ApplyMiCakeConventions(entityTypes);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Add MiCake interceptors if service provider is available
            if (CurrentScopeServices != null)
            {
                optionsBuilder.AddInterceptors(new MiCakeEFCoreInterceptor(CurrentScopeServices));
            }
        }
    }
}
