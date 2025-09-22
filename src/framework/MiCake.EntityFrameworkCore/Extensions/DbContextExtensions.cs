using Microsoft.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Internal;
using System;
using System.Linq;

namespace MiCake.EntityFrameworkCore.Extensions
{
    /// <summary>
    /// Extensions for DbContext to enable MiCake features without inheritance
    /// </summary>
    public static class DbContextExtensions
    {
        /// <summary>
        /// Apply MiCake model conventions to the ModelBuilder
        /// Call this in your DbContext.OnModelCreating method
        /// </summary>
        /// <param name="modelBuilder">The ModelBuilder instance</param>
        public static void UseMiCakeConventions(this ModelBuilder modelBuilder)
        {
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Select(t => t.ClrType)
                .Where(t => t != null)
                .ToArray();
                
            modelBuilder.ApplyMiCakeConventions(entityTypes);
        }
        
        /// <summary>
        /// Configure DbContextOptionsBuilder to use MiCake interceptors
        /// Call this in your DbContext.OnConfiguring method
        /// </summary>
        /// <param name="optionsBuilder">The DbContextOptionsBuilder instance</param>
        /// <param name="serviceProvider">The service provider for dependency injection</param>
        public static void UseMiCakeInterceptors(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
        {
            if (serviceProvider != null)
            {
                optionsBuilder.AddInterceptors(new MiCakeEFCoreInterceptor(serviceProvider));
            }
        }
    }
}