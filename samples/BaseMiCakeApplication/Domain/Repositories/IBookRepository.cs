using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Infrastructure.Paging;
using System;

namespace BaseMiCakeApplication.Domain.Repositories
{
    public interface IBookRepository : IRepositoryHasPagingQuery<Book, Guid>
    {
    }
}
