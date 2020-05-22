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
    }
}
