using MiCake.EntityFrameworkCore.Repository;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Aggregates.Identity;
using TodoApp.Domain.Repositories.Identity;

namespace TodoApp.EFConfiguration.Repositories
{
    public class TodoUserRepository : EFRepository<TodoAppContext, TodoUser>, ITodoUserRepository
    {
        public TodoUserRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<TodoUser?> GetByLoginName(string loginName, CancellationToken cancellationToken = default)
        {
            return await DbSet.FirstOrDefaultAsync(x => x.LoginName == loginName, cancellationToken);
        }
    }
}
