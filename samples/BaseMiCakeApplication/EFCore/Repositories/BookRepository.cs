using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using MiCake.EntityFrameworkCore.Repository;
using System;

namespace BaseMiCakeApplication.EFCore.Repositories
{
    /// <summary>
    /// Repository for Book aggregate root.
    /// </summary>
    /// <remarks>
    /// This repository demonstrates:
    /// 1. Inheritance from EFRepositoryHasPaging for pagination support
    /// 2. Implementation of IBookRepository interface
    /// 3. Automatic CRUD operations from base class
    /// 
    /// Note: Additional custom query methods can be added here as needed.
    /// </remarks>
    public class BookRepository : EFRepositoryHasPaging<BaseAppDbContext, Book, Guid>, IBookRepository
    {
        /// <summary>
        /// Initializes a new instance of the BookRepository.
        /// </summary>
        /// <param name="dependencies">The repository dependencies</param>
        public BookRepository(EFRepositoryDependencies<BaseAppDbContext> dependencies) : base(dependencies)
        {
        }

        // Custom query methods can be added here as needed
        // Example:
        // public async Task<IEnumerable<Book>> GetBooksByAuthor(string authorLastName)
        // {
        //     return await Query(b => b.Author.LastName == authorLastName).ToListAsync();
        // }
    }
}
