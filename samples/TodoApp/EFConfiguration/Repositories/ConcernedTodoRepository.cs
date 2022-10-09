using MiCake.EntityFrameworkCore.Repository;
using TodoApp.Domain.Aggregates.Todo;
using TodoApp.Domain.Repositories.Todo;

namespace TodoApp.EFConfiguration.Repositories
{
    public class ConcernedTodoRepository : EFPaginationRepository<TodoAppContext, ConcernedTodo>, IConcernedTodoRepository
    {
        public ConcernedTodoRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public Task<bool> GetPersonHasConcernedCuurentTodoItem(int userId, int todoId)
        {
            var result = DbSet.FirstOrDefault(s => s.TodoId == todoId && s.UserId == userId) is not null;
            return Task.FromResult(result);
        }
    }
}
