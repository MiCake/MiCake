using MiCake.Core.DependencyInjection;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// Dependency wrapper for EF Core repositories.
    /// Encapsulates all dependencies required by repository implementations to simplify constructor injection.
    /// This follows the MiCake framework's dependency wrapper pattern for classes with multiple dependencies.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type</typeparam>
    public class EFRepositoryDependencies<TDbContext> where TDbContext : DbContext
    {
        /// <summary>
        /// Gets the EF Core context factory for managing DbContext instances
        /// </summary>
        public IEFCoreContextFactory<TDbContext> ContextFactory { get; }

        /// <summary>
        /// Gets the Unit of Work manager for handling transaction boundaries
        /// </summary>
        public IUnitOfWorkManager UnitOfWorkManager { get; }

        /// <summary>
        /// Gets the logger instance for diagnostic logging
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Gets the MiCake EF Core options configuration
        /// </summary>
        public MiCakeEFCoreOptions Options { get; }

        /// <summary>
        /// Creates a new instance of EF repository dependencies wrapper
        /// </summary>
        /// <param name="contextFactory">The EF Core context factory</param>
        /// <param name="unitOfWorkManager">The Unit of Work manager</param>
        /// <param name="logger">The logger for diagnostics</param>
        /// <param name="optionsAccessor">The options accessor for MiCake EF Core configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public EFRepositoryDependencies(
            IEFCoreContextFactory<TDbContext> contextFactory,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<EFRepositoryDependencies<TDbContext>> logger,
            IObjectAccessor<MiCakeEFCoreOptions> optionsAccessor)
        {
            ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            UnitOfWorkManager = unitOfWorkManager ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Options = optionsAccessor?.Value ?? throw new ArgumentNullException(nameof(optionsAccessor));
        }
    }
}
