using MiCake.Audit;
using MiCake.DDD.Domain;

namespace TodoApp.Domain.Aggregates.Todo
{

    public class ConcernedTodo : AggregateRoot, IHasAuditTime, IHasCreatedUser<int>
    {
        public int TodoId { get; protected set; }

        public int UserId { get; protected set; }

        public DateTime CreatedTime
        {
            get; set;
        }

        public DateTime? UpdatedTime
        {
            get; set;
        }

        public int? CreatedBy
        {
            get; set;
        }

        public ConcernedTodo()
        {

        }

        public static ConcernedTodo Create(int todoId, int userId)
        {
            if (todoId <= 0 || userId <= 0)
            {
                throw new System.Exception("todoId or userId is invalid");
            }

            var d = new ConcernedTodo
            {
                TodoId = todoId,
                UserId = userId,
            };

            return new ConcernedTodo
            {
                TodoId = todoId,
                UserId = userId,
            };
        }
    }
}
