using MiCake.Audit;
using MiCake.DDD.Domain;
using TodoApp.Domain.Aggregates.Identity;

namespace TodoApp.Domain.Aggregates.Todo
{
    public class TodoItem : AggregateRoot, IHasAuditTime
    {
        /// <summary>
        /// <see cref="TodoUser"/>
        /// 
        /// <para>
        ///     In DDD, we use primary id to connect two different aggregate root.
        /// </para>
        /// </summary>
        public int AuthorId { get; private set; }

        public string? Title { get; private set; }

        public string? Detail { get; private set; }

        public TodoItemStateType State { get; private set; }

        public DateTime CreatedTime
        {
            get; set;
        }

        public DateTime? UpdatedTime
        {
            get; set;
        }

        public static TodoItem Create(string title, string? detail, int authorId)
        {
            if (authorId == default || authorId < 0)
            {
                throw new DomainException("you must specify author info.");
            }

            if (string.IsNullOrEmpty(title))
            {
                throw new DomainException("must provide a title.");
            }

            return new TodoItem
            {
                AuthorId = authorId,
                Title = title,
                Detail = detail,
                State = TodoItemStateType.Waiting
            };
        }

        /// <summary>
        /// Mark current todo item as done.
        /// </summary>
        public void MarkDone()
        {
            State = TodoItemStateType.Done;
        }

        public void ChangeDetail(string? detail)
        {
            Detail = detail;
        }

        public void ChangeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainException("title can not be empty.");
            }
            Title = title;
        }
    }
}
