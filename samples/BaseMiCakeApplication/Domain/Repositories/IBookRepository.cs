using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Repositories
{
    /// <summary>
    /// Repository interface for Book aggregate root with pagination support.
    /// </summary>
    /// <remarks>
    /// This interface extends IRepositoryHasPagingQuery to support pagination queries.
    /// Custom methods can be added here for domain-specific book queries.
    /// </remarks>
    public interface IBookRepository : IRepositoryHasPagingQuery<Book, Guid>
    {
        // Custom methods can be declared here
        // Example:
        // Task<IEnumerable<Book>> GetBooksByAuthorAsync(string lastName);
        // Task<IEnumerable<Book>> GetRecentBooksAsync(int count);
    }
}
