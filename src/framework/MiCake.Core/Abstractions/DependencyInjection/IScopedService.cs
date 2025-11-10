namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Marker interface for scoped service registration.
    /// Services implementing this interface will be automatically registered with scoped lifetime.
    /// </summary>
    public interface IScopedService : IAutoInjectService
    {
    }
}
