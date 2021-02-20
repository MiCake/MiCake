using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using MiCake.EntityFrameworkCore.Repository;
using System;

namespace BaseMiCakeApplication.EFCore.Repositories
{
    public class BookRepository : EFRepository<BaseAppDbContext, Book, Guid>, IBookRepository
    {
        public BookRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}
