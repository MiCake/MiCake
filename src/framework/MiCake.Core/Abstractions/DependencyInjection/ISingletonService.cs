namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Marker interface for singleton service registration.
    /// Services implementing this interface will be automatically registered with singleton lifetime.
    /// </summary>
    public interface ISingletonService : IAutoInjectService
    {
    }
}
