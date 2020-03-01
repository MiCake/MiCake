namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Specifies the lifetime of a service
    /// </summary>
    public enum MiCakeServiceLifetime
    {
        Singleton = 0,
        Scoped = 1,
        Transient = 2
    }
}
