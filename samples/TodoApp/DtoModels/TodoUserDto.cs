using TodoApp.Domain.Aggregates.Identity;

namespace TodoApp.DtoModels
{
    public class TodoUserDto
    {
        public int Id { get; set; }
        public string? LoginName { get; set; }
        public UserName? Name { get; set; }
    }
}
