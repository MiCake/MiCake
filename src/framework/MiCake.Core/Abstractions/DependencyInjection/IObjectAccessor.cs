namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Provides access to a wrapped object instance through the dependency injection container.
    /// This is useful for registering configuration objects or other values that need to be 
    /// accessed as dependencies.
    /// </summary>
    /// <typeparam name="T">The type of object to wrap and access</typeparam>
    public interface IObjectAccessor<T>
    {
        /// <summary>
        /// Gets the wrapped object value.
        /// </summary>
        T Value { get; }
    }
}
