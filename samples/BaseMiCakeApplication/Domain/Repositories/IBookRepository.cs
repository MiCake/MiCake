using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Domain;
using MiCake.DDD.Extensions.Paging;
using System;

namespace BaseMiCakeApplication.Domain.Repositories
{
    public interface IBookRepository : IRepositoryHasPagingQuery<Book, Guid>
    {
    }
}
