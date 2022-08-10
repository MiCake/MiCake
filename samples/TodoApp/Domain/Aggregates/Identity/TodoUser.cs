using MiCake.AspNetCore.Identity;
using TodoApp.Helper;

namespace TodoApp.Domain.Aggregates.Identity
{
    public class TodoUser : MiCakeUser<int>
    {
        public UserName? Name { get; private set; }

        public string? LoginName { get; private set; }

        public string? Password { get; private set; }

        public static TodoUser Create(string loginName, string password)
        {
            if (string.IsNullOrWhiteSpace(loginName) || string.IsNullOrWhiteSpace(password))
            {
                throw new DomainException("loginName and password can not be empty.");
            }

            return new TodoUser
            {
                LoginName = loginName,
                Password = HashHelper.MD5Encrypt(password)
            };
        }

        public void ChangeUserName(string? firstName, string? lastName)
        {
            Name = UserName.Create(firstName, lastName);
        }

        public bool CheckPassword(string input)
        {
            return HashHelper.MD5Verify(input, Password!);
        }
    }
}
