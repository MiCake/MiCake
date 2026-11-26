using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MiCake.DDD.Domain.Helper;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.Util.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            modelBuilder.UseMiCakeConventions(null);
        }

        /// <summary>
        /// Apply MiCake model conventions to the ModelBuilder and report convention failures
        /// to the loggers created by <paramref name="loggerFactory"/>.
        /// Call this in your DbContext.OnModelCreating method.
        /// </summary>
        /// <param name="modelBuilder">The ModelBuilder instance</param>
        /// <param name="loggerFactory">
        /// Factory used for convention diagnostics. When null, the logger registered by the MiCake
        /// application is used, and diagnostics are discarded if none is available.
        /// </param>
        public static void UseMiCakeConventions(this ModelBuilder modelBuilder, ILoggerFactory? loggerFactory)
        {
            var domainEntityTypes = GetDomainEntityTypes(modelBuilder);

            if (domainEntityTypes.Length > 0)
            {
                var logger = loggerFactory?.CreateLogger(MiCakeConventionEngineProvider.LoggerCategory);
                ModelBuilderExtensions.ApplyConventions(modelBuilder, domainEntityTypes, logger);
            }

            // EFCore will cache the all model configurations, we can clear our internal cache safely.
            ModelBuilderExtensions.ClearConventionCache();
            ClearDomainEntityTypeCache();
        }

        /// <summary>
        /// Finds a logger factory that reaches the application's log sinks: the factory configured
        /// with UseLoggerFactory, or the one from the application service provider of AddDbContext.
        /// EF Core's own internal factory is never returned because it has no sinks.
        /// Returns null when neither exists or EF Core's options cannot be read.
        /// </summary>
        internal static ILoggerFactory? ResolveLoggerFactory(DbContext context)
        {
            try
            {
                var coreOptions = context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>();
                return coreOptions?.LoggerFactory
                    ?? coreOptions?.ApplicationServiceProvider?.GetService<ILoggerFactory>();
            }
            catch (Exception)
            {
                return null;
            }
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
        /// Enables MiCake runtime features for this DbContext by installing the per-context
        /// options (save-operation state and max save cycles). Interceptors are installed
        /// automatically by the MiCake EF Core module via ConfigureDbContext, so no provider
        /// is required here. Call this in AddDbContext configuration or DbContext.OnConfiguring.
        /// </summary>
        /// <param name="optionsBuilder">The DbContextOptionsBuilder instance</param>
        /// <returns>The same DbContextOptionsBuilder for chaining</returns>
        public static DbContextOptionsBuilder UseMiCake(this DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.Options.FindExtension<MiCakeSaveOperationOptionsExtension>() != null)
            {
                return optionsBuilder;
            }

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new MiCakeSaveOperationOptionsExtension());

            return optionsBuilder;
        }
    }
}