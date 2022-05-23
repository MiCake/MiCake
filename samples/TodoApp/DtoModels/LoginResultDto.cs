namespace TodoApp.DtoModels
{
    public class LoginResultDto
    {
        public string AccessToken { get; set; } = string.Empty;

        public TodoUserDto? User { get; set; }
    }
}
