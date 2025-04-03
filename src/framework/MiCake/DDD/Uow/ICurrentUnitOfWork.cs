using Microsoft.Extensions.DependencyInjection;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// A current unit of work accessor.
    /// </summary>
    public interface ICurrentUnitOfWork
    {
        /// <summary>
        /// Current <see cref="IUnitOfWork"/> in this <see cref="IServiceScope"/>
        /// </summary>
        IUnitOfWork Value { get; }
    }
}
