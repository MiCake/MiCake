using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Domain;

namespace BaseMiCakeApplication.Domain.Repositories
{
    /// <summary>
    /// Repository interface for User aggregate root.
    /// </summary>
    /// <remarks>
    /// This interface extends IRepository with domain-specific operations.
    /// Custom methods can be added here for domain-specific queries.
    /// </remarks>
    public interface IUserRepository : IRepository<User, long>
    {
        // Custom methods can be declared here
        // Example:
        // Task<User> FindByPhoneAsync(string phone);
        // Task<IEnumerable<User>> FindActiveUsersAsync();
    }
}
