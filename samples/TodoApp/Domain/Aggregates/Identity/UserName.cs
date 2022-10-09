using MiCake.DDD.Domain;

namespace TodoApp.Domain.Aggregates.Identity
{
    public record UserName(string? FirstName, string? LastName) : RecordValueObject
    {
        public static UserName Create(string firstName, string lastName)
        {
            return new UserName(firstName, lastName);
        }
    }
}
