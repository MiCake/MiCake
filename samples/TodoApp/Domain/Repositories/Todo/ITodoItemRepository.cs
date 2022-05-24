using TodoApp.Domain.Aggregates.Todo;

namespace TodoApp.Domain.Repositories.Todo
{
    public interface ITodoItemRepository : IPaginationRepository<TodoItem, int>
    {
        Task<List<TodoItem>> GetUserTodoItems(int authorId, CancellationToken cancellationToken = default);
    }
}
