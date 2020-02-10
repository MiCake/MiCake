using MiCake.DDD.Extensions.LifeTimes;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// Provide a life cycle interface of repository operation process
    /// Only For EF Core.
    /// </summary>
    public interface IEfRepositoryPreSaveChanges : IRepositoryPreSaveChanges
    {
    }
}
