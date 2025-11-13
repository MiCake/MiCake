using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Repositories
{
    public interface IBookRepository : IRepositoryHasPagingQuery<Book, Guid>
    {
    }
}
