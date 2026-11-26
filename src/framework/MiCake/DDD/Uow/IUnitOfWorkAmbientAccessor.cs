using System;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Host-local accessor for the ambient unit of work frame stack.
    /// Registered as a singleton so host-level components (for example the EF Core
    /// interceptors) can locate the provider of the scope that owns the current unit of
    /// work without resolving scoped services from a root-equivalent provider — which
    /// fails when the host validates scopes (ValidateScopes) and leaks shared state
    /// otherwise.
    /// Framework-provided singleton: the framework registers the authoritative mapping,
    /// and hosts must not register a replacement. A replacement would desynchronize the
    /// ambient state observed through this interface from the state written by the unit
    /// of work manager.
    /// </summary>
    public interface IUnitOfWorkAmbientAccessor
    {
        /// <summary>
        /// The service provider of the scope that owns the current ambient unit of work,
        /// or <c>null</c> when no live unit of work is active in this execution context.
        /// </summary>
        IServiceProvider? CurrentServiceProvider { get; }
    }
}
