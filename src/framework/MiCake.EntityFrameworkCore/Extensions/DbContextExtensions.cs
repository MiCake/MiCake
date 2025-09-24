using Microsoft.EntityFrameworkCore;
using MiCake.DDD.Domain.Helper;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.Core.Util;
using System;
using System.Linq;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// Extensions for DbContext to enable MiCake features without inheritance.
    /// Designed to be lightweight and non-intrusive per MiCake framework principles.
    /// </summary>
    public static class DbContextExtensions
    {
        // Cache size constants for tuning performance vs memory usage
        private const int DomainEntityTypeCacheSize = 500;
        
        // Bounded cache for domain entity types to improve performance and prevent memory issues
        // Cache size is limited to prevent unbounded growth in long-running applications
        private static readonly BoundedLruCache<string, Type[]> _domainEntityTypeCache = new(maxSize: DomainEntityTypeCacheSize);

        /// <summary>
        /// Apply MiCake model conventions to the ModelBuilder.
        /// Call this in your DbContext.OnModelCreating method.
        /// </summary>
        /// <param name="modelBuilder">The ModelBuilder instance</param>
        public static void UseMiCakeConventions(this ModelBuilder modelBuilder)
        {
            var domainEntityTypes = GetDomainEntityTypes(modelBuilder);

            if (domainEntityTypes.Length > 0)
            {
                modelBuilder.ApplyMiCakeConventions(domainEntityTypes);
            }

            // EFCore will cache the all model configurations, we can clear our internal cache safely.
            ModelBuilderExtensions.ClearConventionCache();
            ClearDomainEntityTypeCache();
        }

        /// <summary>
        /// Get domain entity types from the model using MiCake's DomainTypeHelper.
        /// Results are cached using bounded LRU cache for performance optimization in scoped DbContext scenarios.
        /// </summary>
        /// <param name="modelBuilder">The ModelBuilder instance</param>
        /// <returns>Array of domain entity types</returns>
        private static Type[] GetDomainEntityTypes(ModelBuilder modelBuilder)
        {
            // Create cache key from entity types in the model
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Select(t => t.ClrType)
                .Where(t => t != null && IsValidDomainType(t))
                .OrderBy(t => t.FullName)
                .ToArray();

            if (entityTypes.Length == 0)
                return [];

            // Create a more robust cache key using hash of all type names
            var typeNamesHash = string.Join("|", entityTypes.Select(t => t.FullName)).GetHashCode();
            var cacheKey = $"D_{typeNamesHash}_{entityTypes.Length}";

            return _domainEntityTypeCache.GetOrAdd(cacheKey, _ =>
            {
                // Filter to only include domain entity types (entities and aggregate roots)
                return [.. entityTypes];
            });
        }

        private static bool IsValidDomainType(Type type)
        {
            return type != null && (DomainTypeHelper.IsEntity(type) || DomainTypeHelper.IsAggregateRoot(type) || DomainTypeHelper.IsValueObject(type));
        }

        /// <summary>
        /// Clear the domain entity type cache (useful for testing or dynamic scenarios)
        /// </summary>
        public static void ClearDomainEntityTypeCache()
        {
            _domainEntityTypeCache.Clear();
        }

        /// <summary>
        /// Configure DbContextOptionsBuilder to use MiCake interceptors.
        /// Uses the globally configured MiCake interceptor factory.
        /// Call this in AddDbContext configuration or DbContext.OnConfiguring method.
        /// </summary>
        /// <param name="optionsBuilder">The DbContextOptionsBuilder instance</param>
        /// <returns>The same DbContextOptionsBuilder for chaining</returns>
        public static DbContextOptionsBuilder UseMiCakeInterceptors(this DbContextOptionsBuilder optionsBuilder)
        {
            if (!MiCakeInterceptorFactoryHelper.IsConfigured)
            {
                return optionsBuilder;
            }

            var interceptor = MiCakeInterceptorFactoryHelper.CreateInterceptor();
            if (interceptor != null)
            {
                optionsBuilder.AddInterceptors(interceptor);
            }

            return optionsBuilder;
        }

        /// <summary>
        /// Configure DbContextOptionsBuilder to use MiCake interceptors with specific lifetime service.
        /// This overload provides direct control over the lifetime service instance.
        /// </summary>
        /// <param name="optionsBuilder">The DbContextOptionsBuilder instance</param>
        /// <param name="saveChangesLifetime">The save changes lifetime service</param>
        /// <returns>The same DbContextOptionsBuilder for chaining</returns>
        public static DbContextOptionsBuilder UseMiCakeInterceptors(
            this DbContextOptionsBuilder optionsBuilder,
            IEFSaveChangesLifetime saveChangesLifetime)
        {
            if (saveChangesLifetime != null)
            {
                optionsBuilder.AddInterceptors(new MiCakeEFCoreInterceptor(saveChangesLifetime));
            }

            return optionsBuilder;
        }
    }
}