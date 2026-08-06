using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// Non-generic factory contract used for typed immediate transaction initialization
    /// without reflective best-effort probing.
    /// </summary>
    public interface IEFCoreContextFactory
    {
        /// <summary>
        /// Gets the EF Core resource wrapper for the current unit of work.
        /// The same ambient frame and DbContext type always resolve the same wrapper instance.
        /// </summary>
        EFCoreDbContextWrapper GetDbContextWrapper();
    }

    /// <summary>
    /// Generic factory contract for resolving the stable DbContext of the current unit of work.
    /// </summary>
    public interface IEFCoreContextFactory<TDbContext> : IEFCoreContextFactory
        where TDbContext : DbContext
    {
        /// <summary>
        /// Gets the DbContext for the current unit of work.
        /// </summary>
        TDbContext GetDbContext();
    }

    /// <summary>
    /// Implementation of EF Core context factory with frame-stable context identity.
    /// The same logical unit of work (root UoW) and DbContext type always resolve the same
    /// context and resource instance; a resource bound to one live unit of work is rejected
    /// for reuse by another live unit of work.
    /// </summary>
    public class EFCoreContextFactory<TDbContext> : IEFCoreContextFactory<TDbContext>
        where TDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ILogger<EFCoreContextFactory<TDbContext>> _logger;
        private readonly MiCakeEFCoreOptions _efCoreOptions;

        // Frame-stable identity keyed by the ROOT UoW id: shared nested UoWs share the root identity,
        // while requiresNew and standalone scopes own their root and therefore their own context.
        private readonly Dictionary<Guid, (IUnitOfWork Root, EFCoreDbContextWrapper Wrapper)> _wrapperByRoot = new();
        private readonly Lock _lock = new();

        /// <summary>
        /// Creates a new EF Core context factory.
        /// </summary>
        public EFCoreContextFactory(
            IServiceProvider serviceProvider,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<EFCoreContextFactory<TDbContext>> logger,
            IObjectAccessor<MiCakeEFCoreOptions> efCoreOptions)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _unitOfWorkManager = unitOfWorkManager ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _efCoreOptions = efCoreOptions?.Value ?? throw new ArgumentNullException(nameof(efCoreOptions));
        }

        /// <summary>
        /// Gets the DbContext for the current unit of work.
        /// </summary>
        public TDbContext GetDbContext()
        {
            var wrapper = GetDbContextWrapper();
            return (TDbContext)wrapper.DbContext;
        }

        /// <summary>
        /// Gets the DbContext wrapper for the current unit of work.
        /// If <see cref="MiCakeEFCoreOptions.BypassUnitOfWorkCheck"/> is enabled and no UoW is active,
        /// returns a standalone DbContext wrapper without UoW integration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no active Unit of Work is found and <see cref="MiCakeEFCoreOptions.BypassUnitOfWorkCheck"/> is <c>false</c>,
        /// or when the DbContext is already bound to another live unit of work.
        /// </exception>
        public EFCoreDbContextWrapper GetDbContextWrapper()
        {
            var currentUow = _unitOfWorkManager.Current;

            // Check if UoW is required but not present
            if (currentUow == null)
            {
                if (!_efCoreOptions.BypassUnitOfWorkCheck)
                {
                    throw new InvalidOperationException(
                        $"No active Unit of Work found. Please ensure you're within a Unit of Work scope when accessing {typeof(TDbContext).Name}. " +
                        "You can create one using: using var uow = unitOfWorkManager.Begin(); " +
                        $"If you intentionally want to access DbContext without UoW (e.g., for read-only queries in ResourceFilter/Middleware), " +
                        $"set {nameof(MiCakeEFCoreOptions)}.{nameof(MiCakeEFCoreOptions.BypassUnitOfWorkCheck)} = true.");
                }

                // Bypass mode: return a standalone DbContext without UoW integration.
                _logger.LogWarning(
                    "Accessing DbContext {DbContextType} without active Unit of Work (BypassUnitOfWorkCheck is enabled). " +
                    "Ensure this is intentional and only used for read-only operations.",
                    typeof(TDbContext).Name);

                return CreateStandaloneWrapper();
            }

            var root = ResolveRoot(currentUow);

            lock (_lock)
            {
                if (_wrapperByRoot.TryGetValue(root.Id, out var cached))
                {
                    return cached.Wrapper;
                }

                // Ownership validation: drop entries whose root is no longer live, and reject
                // reuse of a resource that is still bound to another live unit of work.
                var staleRoots = new List<Guid>();
                foreach (var (boundRootId, entry) in _wrapperByRoot)
                {
                    if (boundRootId == root.Id)
                    {
                        continue;
                    }

                    if (entry.Root.IsCompleted || entry.Root.IsDisposed)
                    {
                        staleRoots.Add(boundRootId);
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"DbContext {typeof(TDbContext).Name} is already bound to live unit of work {boundRootId} " +
                        $"and cannot be reused by unit of work {root.Id}. " +
                        "Use ExecuteRequiresNewAsync or a separate DI scope for isolated persistence.");
                }

                foreach (var staleRootId in staleRoots)
                {
                    _wrapperByRoot.Remove(staleRootId);
                }

                var wrapper = CreateUoWBoundWrapper(root);
                _wrapperByRoot[root.Id] = (root, wrapper);
                return wrapper;
            }
        }

        /// <summary>
        /// Creates a standalone DbContext wrapper without UoW integration.
        /// Used when BypassUnitOfWorkCheck is enabled and no UoW is active.
        /// </summary>
        private EFCoreDbContextWrapper CreateStandaloneWrapper()
        {
            var dbContext = ResolveDbContext();
            var wrapperLogger = _serviceProvider.GetRequiredService<ILogger<EFCoreDbContextWrapper>>();

            // Don't dispose DbContext since it's managed by the DI container.
            return new EFCoreDbContextWrapper(dbContext, wrapperLogger, shouldDisposeDbContext: false);
        }

        /// <summary>
        /// Creates a DbContext wrapper bound to the specified root unit of work and registers it.
        /// </summary>
        private EFCoreDbContextWrapper CreateUoWBoundWrapper(IUnitOfWork root)
        {
            var dbContext = ResolveDbContext();
            var wrapperLogger = _serviceProvider.GetRequiredService<ILogger<EFCoreDbContextWrapper>>();

            // Don't dispose DbContext since it's managed by the DI container.
            var wrapper = new EFCoreDbContextWrapper(dbContext, wrapperLogger, shouldDisposeDbContext: false);

            if (root is IUnitOfWorkInternal internalUow)
            {
                internalUow.RegisterResource(wrapper);
                _logger.LogDebug(
                    "Registered EFCore DbContext wrapper for {DbContextType} with resource {ResourceId} in UoW {UowId}",
                    typeof(TDbContext).Name, wrapper.Id, root.Id);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The active unit of work {root.Id} does not implement {nameof(IUnitOfWorkInternal)} and cannot own persistence resources. " +
                    "Use the MiCake unit of work manager (BeginAsync / ExecuteRequiresNewAsync) instead of a foreign IUnitOfWork implementation.");
            }

            return wrapper;
        }

        private TDbContext ResolveDbContext()
        {
            try
            {
                return _serviceProvider.GetRequiredService<TDbContext>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve {typeof(TDbContext).Name} from dependency injection. " +
                    "Ensure the DbContext is properly registered in the DI container.", ex);
            }
        }

        private static IUnitOfWork ResolveRoot(IUnitOfWork unitOfWork)
        {
            var current = unitOfWork;
            while (current.Parent != null)
            {
                current = current.Parent;
            }

            return current;
        }
    }
}
