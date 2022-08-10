using TodoApp.Domain.Aggregates.Todo;

namespace TodoApp.DtoModels
{
    public class TodoItemDto
    {
        public int Id { get; set; }

        public int AuthorId { get; set; }

        public string? Title { get; set; }

        public string? Detail { get; set; }

        public DateTime CreationTime { get; set; }

        public DateTime? ModificationTime { get; set; }

        public TodoItemStateType State { get; set; }
    }

    public class CreateTodoItemDto
    {
        public string? Title { get; set; }

        public string? Detail { get; set; }
    }
}
