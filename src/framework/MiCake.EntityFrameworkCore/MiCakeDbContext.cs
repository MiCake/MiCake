using Microsoft.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Extensions;
using System;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// Base DbContext class with MiCake features pre-configured.
    /// You can inherit from this class, or use the extension methods in your own DbContext.
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
            
            // Apply MiCake conventions using extension method
            modelBuilder.UseMiCakeConventions();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Add MiCake interceptors using extension method
            optionsBuilder.UseMiCakeInterceptors(CurrentScopeServices);
        }
    }
}
