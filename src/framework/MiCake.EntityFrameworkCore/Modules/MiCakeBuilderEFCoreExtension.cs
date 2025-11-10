using MiCake.Audit;
using MiCake.Audit.Conventions;
using MiCake.Core;
using MiCake.Core.DependencyInjection;
using MiCake.DDD.Extensions.Store;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Modules;
using MiCake.EntityFrameworkCore.Options;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace MiCake.EntityFrameworkCore
{
    public static class MiCakeBuilderEFCoreExtension
    {
        /// <summary>
        /// Add MiCake EFCore services with default conventions.
        /// </summary>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseEFCore<TDbContext>(this IMiCakeBuilder builder)
            where TDbContext : DbContext
        {
            return UseEFCore<TDbContext>(builder, null, null);
        }

        /// <summary>
        /// Add MiCake EFCore services with configurable conventions.
        /// </summary>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="conventionBuilder">Optional configuration for EF Core conventions</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseEFCore<TDbContext>(
            this IMiCakeBuilder builder,
            Action<MiCakeEFCoreConventionOptions> conventionBuilder)
            where TDbContext : DbContext
        {
            return UseEFCore<TDbContext>(builder, conventionBuilder, null);
        }

        /// <summary>
        /// Add MiCake EFCore services with configurable conventions and options.
        /// </summary>
        /// <typeparam name="TDbContext"><see cref="MiCakeDbContext"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="conventionBuilder">Optional configuration for EF Core conventions</param>
        /// <param name="optionsBuilder">The config for MiCake EFCore extension</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseEFCore<TDbContext>(
            this IMiCakeBuilder builder,
            Action<MiCakeEFCoreConventionOptions> conventionBuilder,
            Action<MiCakeEFCoreOptions> optionsBuilder)
            where TDbContext : DbContext
        {
            return UseEFCore(builder, typeof(TDbContext), conventionBuilder, optionsBuilder);
        }

        /// <summary>
        /// Enable Entity Framework Core integration without any conventions
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type to configure</typeparam>
        /// <param name="builder">The MiCake builder instance</param>
        /// <param name="optionsBuilder">Optional configuration for MiCake EF Core options</param>
        /// <returns>The MiCake builder for method chaining</returns>
        public static IMiCakeBuilder UseEFCoreWithoutConventions<TDbContext>(
            this IMiCakeBuilder builder,
            Action<MiCakeEFCoreOptions> optionsBuilder = null)
            where TDbContext : DbContext
        {
            return UseEFCore<TDbContext>(builder, conventions => conventions.ClearConventions(), optionsBuilder);
        }

        /// <summary>
        /// Enable Entity Framework Core integration with only specific conventions
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type to configure</typeparam>
        /// <param name="builder">The MiCake builder instance</param>
        /// <param name="conventionBuilder">Configuration for specific conventions</param>
        /// <param name="optionsBuilder">Optional configuration for MiCake EF Core options</param>
        /// <returns>The MiCake builder for method chaining</returns>
        public static IMiCakeBuilder UseEFCoreWithConventions<TDbContext>(
            this IMiCakeBuilder builder,
            Action<MiCakeEFCoreConventionOptions> conventionBuilder,
            Action<MiCakeEFCoreOptions> optionsBuilder = null)
            where TDbContext : DbContext
        {
            return UseEFCore<TDbContext>(builder, conventions =>
            {
                conventions.ClearConventions(); // Start with clean slate
                conventionBuilder?.Invoke(conventions);
            }, optionsBuilder);
        }

        /// <summary>
        /// Add MiCake EFCore services (backward compatibility).
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeDbContextType"><see cref="MiCakeDbContext"/></param>
        /// <param name="optionsBuilder">The config for MiCake EFCore extension</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseEFCore(
            this IMiCakeBuilder builder,
            Type miCakeDbContextType,
            Action<MiCakeEFCoreOptions> optionsBuilder)
        {
            return UseEFCore(builder, miCakeDbContextType, null, optionsBuilder);
        }

        /// <summary>
        /// Add MiCake EFCore services.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeDbContextType"><see cref="MiCakeDbContext"/></param>
        /// <param name="conventionBuilder">Optional configuration for EF Core conventions</param>
        /// <param name="optionsBuilder">The config for MiCake EFCore extension</param>
        /// <returns><see cref="IMiCakeBuilder"/></returns>
        public static IMiCakeBuilder UseEFCore(
            this IMiCakeBuilder builder,
            Type miCakeDbContextType,
            Action<MiCakeEFCoreConventionOptions> conventionBuilder,
            Action<MiCakeEFCoreOptions> optionsBuilder)
        {
            MiCakeEFCoreOptions options = new(miCakeDbContextType);
            optionsBuilder?.Invoke(options);

            // Register services directly on the builder's service collection
            builder.Services.AddSingleton<IObjectAccessor<MiCakeEFCoreOptions>>(options);
            
            // Add EFCore UoW services
            builder.Services.AddUowCoreServices(miCakeDbContextType);

            // Configure conventions
            ConfigureConventionsDirectly(conventionBuilder, builder.Services);

            // Configure the MiCake interceptor factory
            ConfigureInterceptorFactoryDirectly(builder.Services);

            // MiCakeEFCoreModule should be added through module dependency ([RelyOn] attribute)

            return builder;
        }

        /// <summary>
        /// Configure conventions using service collection instead of application instance.
        /// This allows convention setup during build phase without needing runtime application context.
        /// </summary>
        private static void ConfigureConventionsDirectly(Action<MiCakeEFCoreConventionOptions> conventionBuilder, IServiceCollection services)
        {
            var conventionOptions = new MiCakeEFCoreConventionOptions();

            // Add default conventions if no custom configuration provided
            if (conventionBuilder == null)
            {
                AddDefaultConventionsCore(conventionOptions, services);
            }
            else
            {
                conventionBuilder(conventionOptions);
            }

            // Create and configure the convention engine
            var engine = CreateConventionEngine(conventionOptions);
            MiCakeConventionEngineProvider.SetConventionEngine(engine);
        }

        private static void ConfigureConventions(Action<MiCakeEFCoreConventionOptions> conventionBuilder, IMiCakeApplication app)
        {
            var conventionOptions = new MiCakeEFCoreConventionOptions();

            // Add default conventions if no custom configuration provided
            if (conventionBuilder == null)
            {
                AddDefaultConventionsCore(conventionOptions, app);
            }
            else
            {
                conventionBuilder(conventionOptions);
            }

            // Create and configure the convention engine
            var engine = CreateConventionEngine(conventionOptions);
            MiCakeConventionEngineProvider.SetConventionEngine(engine);
        }

        public static MiCakeEFCoreConventionOptions AddDefaultConventions(this MiCakeEFCoreConventionOptions conventionOptions, IMiCakeApplication app)
        {
            AddDefaultConventionsCore(conventionOptions, app);
            return conventionOptions;
        }

        private static void AddDefaultConventionsCore(MiCakeEFCoreConventionOptions conventionOptions, IServiceCollection services)
        {
            // Retrieve audit options from service collection via Configure pattern
            // Note: This assumes options were configured via builder.Services.Configure<MiCakeApplicationOptions>
            var serviceProvider = services.BuildServiceProvider();
            var appOptions = serviceProvider.GetService<IOptions<MiCakeApplicationOptions>>();
            
            var auditOps = (MiCakeAuditOptions)appOptions?.Value.BuildTimeData.TakeOut(MiCakeBuilderAuditCoreExtension.AuditForApplicationOptionsKey);

            if (auditOps == null || !auditOps.UseAudit)
                return;

            conventionOptions.AddConvention(new AuditTimeConvention());

            if (auditOps.UseSoftDeletion)
                conventionOptions.AddConvention(new SoftDeletionConvention());
        }

        private static void AddDefaultConventionsCore(MiCakeEFCoreConventionOptions conventionOptions, IMiCakeApplication app)
        {
            var auditOps = (MiCakeAuditOptions)app.ApplicationOptions.BuildTimeData.TakeOut(MiCakeBuilderAuditCoreExtension.AuditForApplicationOptionsKey);

            if (auditOps == null || !auditOps.UseAudit)
                return;

            conventionOptions.AddConvention(new AuditTimeConvention());

            if (auditOps.UseSoftDeletion)
                conventionOptions.AddConvention(new SoftDeletionConvention());
        }

        private static StoreConventionEngine CreateConventionEngine(MiCakeEFCoreConventionOptions conventionOptions)
        {
            var engine = new StoreConventionEngine();

            // Register all configured conventions
            foreach (var convention in conventionOptions.Conventions)
            {
                engine.AddConvention(convention);
            }

            return engine;
        }

        /// <summary>
        /// Configure the MiCake interceptor factory during build phase.
        /// This registers the factory directly without needing application instance.
        /// </summary>
        /// <param name="services">The service collection</param>
        private static void ConfigureInterceptorFactoryDirectly(IServiceCollection services)
        {
            // Register the interceptor factory as a singleton service
            // The factory will use the DI container properly without creating additional ServiceProviders
            services.AddSingleton<IMiCakeInterceptorFactory, MiCakeInterceptorFactory>();
        }

        /// <summary>
        /// Configure the MiCake interceptor factory with proper dependency injection pattern.
        /// This approach avoids memory leaks by using proper service registration instead of creating ServiceProvider instances.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="app">The MiCake application instance</param>
        private static void ConfigureInterceptorFactory(IServiceCollection services, IMiCakeApplication app)
        {
            // Register the interceptor factory as a singleton service
            // The factory will use the DI container properly without creating additional ServiceProviders
            services.AddSingleton<IMiCakeInterceptorFactory, MiCakeInterceptorFactory>();
        }
    }
}
