namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Used to wrap an object for easy access in the dependency injection framework
    /// </summary>
    /// <typeparam name="T">Objects to be wrapped</typeparam>
    public interface IObjectAccessor<T>
    {
        T Value { get; }
    }
}
