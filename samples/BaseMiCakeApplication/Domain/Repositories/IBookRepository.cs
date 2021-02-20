using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Domain.Repositories
{
    public interface IBookRepository : IRepository<Book, Guid>
    {
    }
}
