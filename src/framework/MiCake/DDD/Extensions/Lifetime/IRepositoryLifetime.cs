namespace MiCake.DDD.Extensions.Lifetime
{
    /// <summary>
    /// Provide a life cycle interface of repository operation process
    /// </summary>
    public interface IRepositoryLifetime
    {
        /// <summary>
        /// Order of <see cref="IRepositoryLifetime"/>
        /// 
        /// All IRepositoryLifetime services will be executed according to the order from small to large.
        /// </summary>
        int Order { get; set; }
    }
}
