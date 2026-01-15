using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using MiCake.EntityFrameworkCore.Repository;

namespace BaseMiCakeApplication.EFCore.Repositories
{
    /// <summary>
    /// Repository for User aggregate root.
    /// </summary>
    /// <remarks>
    /// This repository demonstrates:
    /// 1. Inheritance from EFRepository for basic CRUD operations
    /// 2. Implementation of IUserRepository interface
    /// 3. Support for soft deletion through IRepositorySoftDeletionProvider
    /// 
    /// Note: Custom query methods can be added here for domain-specific queries.
    /// </remarks>
    public class UserRepository : EFRepository<BaseAppDbContext, User, long>, IUserRepository
    {
        /// <summary>
        /// Initializes a new instance of the UserRepository.
        /// </summary>
        /// <param name="dependencies">The repository dependencies</param>
        public UserRepository(EFRepositoryDependencies<BaseAppDbContext> dependencies) : base(dependencies)
        {
        }

        // Custom query methods can be added here as needed
        // Example:
        // public async Task<User> FindByPhoneAsync(string phone)
        // {
        //     return await FirstOrDefaultAsync(u => u.Phone == phone);
        // }
    }
}
