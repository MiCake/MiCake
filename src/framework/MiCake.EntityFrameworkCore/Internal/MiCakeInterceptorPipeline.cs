using Microsoft.EntityFrameworkCore;
using MiCake.DDD.Uow;
using System;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Shared pipeline resolution for interceptors: resolves the write coordinator from the
    /// provider of the scope that owns the ambient unit of work and builds the standard
    /// "unavailable pipeline" diagnostics. Interceptors must never resolve scoped services
    /// from the provider captured at options-build time (the pool root under AddDbContextPool).
    /// </summary>
    internal static class MiCakeInterceptorPipeline
    {
        /// <summary>
        /// Resolves the provider of the scope that owns the ambient unit of work, or
        /// <c>null</c> when no ambient unit of work is active in this execution context.
        /// </summary>
        public static IServiceProvider? ResolveCurrentUowServiceProvider(IUnitOfWorkAmbientAccessor ambientAccessor)
            => ambientAccessor.CurrentServiceProvider;

        /// <summary>
        /// Resolves the scoped write coordinator from the provider that owns the ambient
        /// unit of work. Without an ambient unit of work the pipeline is unavailable;
        /// database initialization passes through unguarded.
        /// </summary>
        public static IEFCoreWriteCoordinator? ResolveCoordinator(IUnitOfWorkAmbientAccessor ambientAccessor)
        {
            var frameProvider = ResolveCurrentUowServiceProvider(ambientAccessor);
            if (frameProvider == null)
            {
                return null;
            }

            try
            {
                return (IEFCoreWriteCoordinator?)frameProvider.GetService(typeof(IEFCoreWriteCoordinator));
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }

        public static InvalidOperationException CreateUnavailableException(DbContext context, bool noActiveUow)
            => noActiveUow
                ? new InvalidOperationException(
                    $"Write operation on {context.GetType().Name} requires an active writable unit of work. " +
                    "Begin one with IUnitOfWorkManager.BeginAsync() before saving or executing write commands.")
                : new InvalidOperationException(
                    $"Write operation on {context.GetType().Name} cannot be guarded because the MiCake write pipeline is " +
                    "not registered for this DbContext. Register the MiCake EF Core module (AddMiCake/AddMiCakeWithDefault) " +
                    "and register the DbContext in the container so the interceptor configurator attaches the write pipeline.");
    }
}
