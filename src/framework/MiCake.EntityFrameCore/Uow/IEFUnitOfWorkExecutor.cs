using MiCake.Uow;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace MiCake.EntityFrameworkCore.Uow
{
    /// <summary>
    /// The <see cref="IUnitOfWorkExecutor"/> for EFCore.
    /// </summary>
    public interface IEFUnitOfWorkExecutor : IUnitOfWorkExecutor
    {
        /// <summary>
        /// The collection of DbContext.
        /// </summary>
        public IEnumerable<DbContext> DbContexts { get; }


        /// <summary>
        /// Add DBContext instance to this executor.
        /// These DbContext will execute in unit of work.
        /// </summary>
        /// <param name="dbContext"></param>
        void AddDbContext(DbContext dbContext);
    }
}
