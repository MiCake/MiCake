namespace TodoApp.DtoModels
{
    public class LoginResultDto
    {
        public string? RefreshToken { get; set; }

        public string AccessToken { get; set; } = string.Empty;

        public TodoUserDto? User { get; set; }
    }
}
