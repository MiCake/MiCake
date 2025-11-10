namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Base marker interface for automatic service registration.
    /// Classes implementing this interface or its derived interfaces (ITransientService, IScopedService, ISingletonService)
    /// will be automatically registered in the dependency injection container.
    /// </summary>
    public interface IAutoInjectService
    {
    }
}
