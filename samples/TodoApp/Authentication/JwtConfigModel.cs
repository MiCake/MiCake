namespace TodoApp.Authentication
{
    public class JwtConfigModel
    {
        public string? SecurityKey { get; set; }

        public string? Issuer { get; set; }

        public string? Audience { get; set; }

        public int AccessTokenLifetime { get; set; }
    }
}
