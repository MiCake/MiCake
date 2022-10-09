namespace TodoApp.DtoModels
{
    public class CreateUserDto
    {
        public string LoginName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}
