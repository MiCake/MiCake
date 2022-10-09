using TodoApp.Domain.Aggregates.Todo;

namespace TodoApp.Domain.Repositories.Todo
{
    public interface IConcernedTodoRepository : IPaginationRepository<ConcernedTodo, int>
    {
        Task<bool> GetPersonHasConcernedCuurentTodoItem(int userId, int todoId);
    }
}
