using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// A DbContext instance together with the resource wrapper resolved for it in the
    /// current unit of work. Carries the exact context identity so framework consumers can
    /// validate that the wrapper is anchored to the context that performs the work.
    /// </summary>
    internal readonly record struct EFCoreContextResourceResolution(DbContext Context, EFCoreDbContextWrapper Wrapper);

    /// <summary>
    /// Internal runtime view of the context factory used by framework components that
    /// resolve factories by a runtime DbContext type. Not part of the public contract.
    /// </summary>
    internal interface IEFCoreContextFactory
    {
        /// <summary>
        /// Gets or creates the wrapper anchored to the given DbContext instance in the
        /// current unit of work.
        /// </summary>
        EFCoreDbContextWrapper GetOrCreateWrapperFor(DbContext context);

        /// <summary>
        /// Gets the DbContext and its wrapper for the current unit of work; when bypass is
        /// enabled and no unit of work is active, returns a standalone wrapper.
        /// </summary>
        EFCoreContextResourceResolution GetOrCreateWrapperForCurrentUnitOfWork();
    }

    /// <summary>
    /// Factory contract for resolving the frame-stable DbContext of the current unit of
    /// work and anchoring a resource wrapper to an exact DbContext instance.
    /// </summary>
    public interface IEFCoreContextFactory<TDbContext> where TDbContext : DbContext
    {
        /// <summary>
        /// Gets the frame-stable DbContext for the current unit of work.
        /// </summary>
        TDbContext GetDbContext();

        /// <summary>
        /// Gets or creates the EF Core resource wrapper for the given DbContext instance in
        /// the current unit of work. A wrapper already registered for this context instance
        /// is reused, so the same context never becomes a second unit-of-work resource.
        /// </summary>
        /// <param name="context">The DbContext instance that performs the work</param>
        EFCoreDbContextWrapper GetOrCreateWrapperFor(DbContext context);
    }

    /// <summary>
    /// Implementation of EF Core context factory with frame-stable context identity.
    /// The same logical unit of work (root UoW) and DbContext type always resolve the same
    /// context and resource instance; a resource bound to one live unit of work is rejected
    /// for reuse by another live unit of work.
    /// </summary>
    public class EFCoreContextFactory<TDbContext> : IEFCoreContextFactory<TDbContext>, IEFCoreContextFactory
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
            var resolution = ((IEFCoreContextFactory)this).GetOrCreateWrapperForCurrentUnitOfWork();
            return (TDbContext)resolution.Wrapper.DbContext;
        }

        /// <summary>
        /// Gets the DbContext wrapper for the current unit of work.
        /// If <see cref="MiCakeEFCoreOptions.AllowDbContextAccessWithoutUoW"/> is enabled and no UoW is active,
        /// returns a standalone DbContext wrapper without UoW integration.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no active Unit of Work is found and <see cref="MiCakeEFCoreOptions.AllowDbContextAccessWithoutUoW"/> is <c>false</c>,
        /// or when the DbContext is already bound to another live unit of work.
        /// </exception>
        EFCoreContextResourceResolution IEFCoreContextFactory.GetOrCreateWrapperForCurrentUnitOfWork()
        {
            var currentUow = _unitOfWorkManager.Current;

            // Check if UoW is required but not present
            if (currentUow == null)
            {
                EnsureNoActiveUoWIsAllowed();
                var bypassContext = ResolveDbContext();
                return new EFCoreContextResourceResolution(bypassContext, CreateStandaloneWrapper(bypassContext));
            }

            var root = ResolveRoot(currentUow);
            var dbContext = ResolveDbContext();

            return new EFCoreContextResourceResolution(dbContext, GetOrCreateWrapperFor(root, dbContext));
        }

        /// <summary>
        /// Gets or creates the wrapper for the given DbContext instance in the current unit
        /// of work. The wrapper is anchored to the exact context instance: when the UoW
        /// already owns a wrapper for this context, it is reused, so one context instance
        /// never becomes two resources (which would activate two transactions on one store).
        /// </summary>
        public EFCoreDbContextWrapper GetOrCreateWrapperFor(DbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var currentUow = _unitOfWorkManager.Current;

            if (currentUow == null)
            {
                EnsureNoActiveUoWIsAllowed();
                return CreateStandaloneWrapper(context);
            }

            var root = ResolveRoot(currentUow);
            return GetOrCreateWrapperFor(root, context);
        }

        private EFCoreDbContextWrapper GetOrCreateWrapperFor(IUnitOfWork root, DbContext dbContext)
        {
            // Reuse an existing resource that already wraps this exact context instance.
            if (root is IUnitOfWorkInternal internalUow &&
                internalUow.TryGetResource(
                    r => r is EFCoreDbContextWrapper w && ReferenceEquals(w.DbContext, dbContext),
                    out var existing))
            {
                return (EFCoreDbContextWrapper)existing!;
            }

            lock (_lock)
            {
                if (_wrapperByRoot.TryGetValue(root.Id, out var cached))
                {
                    if (!ReferenceEquals(cached.Wrapper.DbContext, dbContext))
                    {
                        throw new InvalidOperationException(
                            $"DbContext {typeof(TDbContext).Name} for unit of work {root.Id} is already bound to a different context instance. " +
                            "A unit of work resolves exactly one DbContext instance per type; the caller captured a DbContext from another scope. " +
                            "Use ExecuteRequiresNewAsync or a separate DI scope for isolated persistence.");
                    }

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

                var wrapper = CreateUoWBoundWrapper(root, dbContext);
                _wrapperByRoot[root.Id] = (root, wrapper);
                return wrapper;
            }
        }

        /// <summary>
        /// Creates a standalone DbContext wrapper without UoW integration.
        /// Used when AllowDbContextAccessWithoutUoW is enabled and no UoW is active.
        /// </summary>
        private EFCoreDbContextWrapper CreateStandaloneWrapper(DbContext dbContext)
        {
            var wrapperLogger = _serviceProvider.GetRequiredService<ILogger<EFCoreDbContextWrapper>>();

            // Don't dispose DbContext since it's managed by the DI container.
            return new EFCoreDbContextWrapper(dbContext, wrapperLogger, shouldDisposeDbContext: false);
        }

        /// <summary>
        /// Creates a DbContext wrapper bound to the specified root unit of work and registers it.
        /// </summary>
        private EFCoreDbContextWrapper CreateUoWBoundWrapper(IUnitOfWork root, DbContext dbContext)
        {
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

        /// <summary>
        /// Enforces the no-UoW policy: without an ambient unit of work the default is to fail
        /// with registration guidance; AllowDbContextAccessWithoutUoW downgrades to a warning and
        /// allows context access. Write guarding applies inside an ambient UoW (Permissive policy
        /// outside one), so relaxed resolution does not weaken UoW-internal guarantees.
        /// </summary>
        private void EnsureNoActiveUoWIsAllowed()
        {
            if (_efCoreOptions.AllowDbContextAccessWithoutUoW)
            {
                _logger.LogWarning(
                    "Accessing DbContext {DbContextType} without active Unit of Work (AllowDbContextAccessWithoutUoW is enabled). " +
                    "Ensure this is intentional and only used for read-only operations.",
                    typeof(TDbContext).Name);
                return;
            }

            throw new InvalidOperationException(
                $"No active Unit of Work found. Please ensure you're within a Unit of Work scope when accessing {typeof(TDbContext).Name}. " +
                "You can create one using: using var uow = unitOfWorkManager.Begin(); " +
                $"If you intentionally want to access DbContext without UoW (e.g., for read-only queries in ResourceFilter/Middleware), " +
                $"set {nameof(MiCakeEFCoreOptions)}.{nameof(MiCakeEFCoreOptions.AllowDbContextAccessWithoutUoW)} = true.");
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

    /// <summary>
    /// Adapts a custom <see cref="IEFCoreContextFactory{TDbContext}"/> implementation to
    /// the internal runtime view by invoking the public interface methods. Used only for
    /// framework-internal consumers that resolve factories by a runtime DbContext type.
    /// </summary>
    internal sealed class EFCoreContextFactoryAdapter : IEFCoreContextFactory
    {
        private static readonly ConcurrentDictionary<(Type FactoryType, string MethodName), System.Reflection.MethodInfo> _methodCache = new();

        private readonly object _factory;
        private readonly System.Reflection.MethodInfo _getOrCreateWrapperFor;
        private readonly System.Reflection.MethodInfo _getDbContext;

        public EFCoreContextFactoryAdapter(object factory, Type factoryType)
        {
            _factory = factory;
            _getOrCreateWrapperFor = GetMethod(factoryType, nameof(IEFCoreContextFactory<DbContext>.GetOrCreateWrapperFor));
            _getDbContext = GetMethod(factoryType, nameof(IEFCoreContextFactory<DbContext>.GetDbContext));
        }

        public EFCoreDbContextWrapper GetOrCreateWrapperFor(DbContext context)
            => (EFCoreDbContextWrapper)Invoke(_getOrCreateWrapperFor, _factory, [context])!;

        public EFCoreContextResourceResolution GetOrCreateWrapperForCurrentUnitOfWork()
        {
            var context = (DbContext)Invoke(_getDbContext, _factory, null)!;
            var wrapper = (EFCoreDbContextWrapper)Invoke(_getOrCreateWrapperFor, _factory, [context])!;
            return new EFCoreContextResourceResolution(context, wrapper);
        }

        private static System.Reflection.MethodInfo GetMethod(Type factoryType, string methodName)
            => _methodCache.GetOrAdd((factoryType, methodName), static key => key.FactoryType.GetMethod(key.MethodName)!);

        private static object Invoke(System.Reflection.MethodInfo method, object factory, object?[]? args)
        {
            try
            {
                return method.Invoke(factory, args)!;
            }
            catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
            {
                // Preserve the original exception raised by the custom factory (e.g. DI
                // resolution or context-identity failures) instead of surfacing it wrapped
                // as a reflection boundary exception.
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                throw; // unreachable
            }
        }
    }

    /// <summary>
    /// Validates that a resolved resource wrapper is anchored to the exact DbContext that
    /// performs the work and registers it with the unit of work. Shared by the write
    /// coordinator and the immediate transaction initializer so custom factories cannot
    /// silently produce unregistered or wrong-context resources.
    /// </summary>
    internal static class EFCoreContextResourceAnchor
    {
        public static EFCoreDbContextWrapper AnchorResource(
            IUnitOfWork unitOfWork,
            DbContext context,
            EFCoreDbContextWrapper wrapper,
            string contextTypeName)
        {
            // The resource must wrap the DbContext that performs the work; otherwise the
            // transaction would be activated on another context and the current write
            // would fail unbound.
            if (!ReferenceEquals(wrapper.DbContext, context))
            {
                throw new InvalidOperationException(
                    $"The context factory for {contextTypeName} returned a resource bound to a different DbContext instance. " +
                    "A unit of work resolves exactly one DbContext instance per type; the factory must return a wrapper for the " +
                    "DbContext that performs the work.");
            }

            if (unitOfWork is not IUnitOfWorkInternal internalUow)
            {
                throw new InvalidOperationException(
                    $"The active unit of work {unitOfWork.Id} does not implement {nameof(IUnitOfWorkInternal)} and cannot own persistence resources. " +
                    "Use the MiCake unit of work manager (BeginAsync / ExecuteRequiresNewAsync) instead of a foreign IUnitOfWork implementation.");
            }

            // Reuse an existing resource that already wraps this exact context instance so a
            // non-caching custom factory cannot register the same context as a second resource
            // (which would activate a second transaction on one store).
            if (internalUow.TryGetResource(
                    r => r is EFCoreDbContextWrapper w && ReferenceEquals(w.DbContext, context),
                    out var existing))
            {
                return (EFCoreDbContextWrapper)existing!;
            }

            // A custom factory may return a wrapper that is not yet registered with the
            // unit of work; registration prepares the resource so it can activate its
            // transaction and participate in commit/rollback. Idempotent for wrappers
            // already registered by the framework factory.
            internalUow.RegisterResource(wrapper);

            return wrapper;
        }
    }
}
