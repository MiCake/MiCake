using MiCake.EntityFrameworkCore.Repository;
using TodoApp.Domain.Aggregates.Todo;
using TodoApp.Domain.Repositories.Todo;

namespace TodoApp.EFConfiguration.Repositories
{
    public class TodoItemRepository : EFPaginationRepository<TodoAppContext, TodoItem>, ITodoItemRepository
    {
        public TodoItemRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public Task<List<TodoItem>> GetUserTodoItems(int authorId, CancellationToken cancellationToken = default)
        {
            var result = DbSet.Where(t => t.AuthorId == authorId).ToList();
            return Task.FromResult(result);
        }
    }
}
