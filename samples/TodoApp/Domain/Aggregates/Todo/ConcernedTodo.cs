using MiCake.Audit;
using MiCake.DDD.Domain;

namespace TodoApp.Domain.Aggregates.Todo
{

    public class ConcernedTodo : AggregateRoot, IHasAudit, IHasCreator<int>
    {
        public int TodoId { get; set; }

        public int UserId { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime? ModificationTime { get; set; }
        public int? CreatorID { get; set; }

        public static ConcernedTodo Create(int todoId, int userId)
        {
            if (todoId <= 0 || userId <= 0)
            {
                throw new System.Exception("todoId or userId is invalid");
            }

            return new ConcernedTodo
            {
                TodoId = todoId,
                UserId = userId,
            };
        }
    }
}
