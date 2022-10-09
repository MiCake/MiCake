using MiCake.DDD.Domain;
using TodoApp.Domain.Aggregates.Identity;

namespace TodoApp.Domain.Repositories.Identity
{
    public interface ITodoUserRepository : IRepository<TodoUser, int>
    {
        Task<TodoUser?> GetByLoginName(string loginName, CancellationToken cancellationToken = default);
    }
}
