using MiCake.DDD.Extensions.LifeTime;

namespace MiCake.EntityFrameworkCore.LifeTime
{
    /// <summary>
    /// Provide a life cycle interface of repository operation process
    /// Only For EF Core.
    /// </summary>
    public interface IEfRepositoryPreSaveChanges : IRepositoryPreSaveChanges
    {
    }
}
