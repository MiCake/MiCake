namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Marker interface for transient service registration.
    /// Services implementing this interface will be automatically registered with transient lifetime.
    /// </summary>
    public interface ITransientService : IAutoInjectService
    {
    }
}
