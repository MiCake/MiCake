using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using MiCake.EntityFrameworkCore.Repository;
using System;

namespace BaseMiCakeApplication.EFCore.Repositories
{
    public class BookRepository : EFRepositoryHasPaging<BaseAppDbContext, Book, Guid>, IBookRepository
    {
        public BookRepository(EFRepositoryDependencies<BaseAppDbContext> dependencies) : base(dependencies)
        {
        }
    }
}
