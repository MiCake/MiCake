using MiCake.Cord.Paging;
using MiCake.DDD.Domain;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Repository
{
    /// <summary>
    /// A default EFRepository use <see cref="int"/> as the primary key.
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <typeparam name="TAggregateRoot"></typeparam>
    public abstract class EFRepository<TDbContext, TAggregateRoot> : EFRepository<TDbContext, TAggregateRoot, int>, IRepository<TAggregateRoot, int>
        where TAggregateRoot : class, IAggregateRoot<int>
        where TDbContext : DbContext
    {
        protected EFRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    /// <summary>
    /// A default readonly EFRepository use <see cref="int"/> as the primary key.
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <typeparam name="TAggregateRoot"></typeparam>
    public abstract class EFReadOnlyRepository<TDbContext, TAggregateRoot> : EFReadOnlyRepository<TDbContext, TAggregateRoot, int>, IReadOnlyRepository<TAggregateRoot, int>
        where TAggregateRoot : class, IAggregateRoot<int>
        where TDbContext : DbContext
    {
        protected EFReadOnlyRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    /// <summary>
    /// A default pagination EFRepository use <see cref="int"/> as the primary key.
    /// </summary>
    /// <typeparam name="TDbContext"></typeparam>
    /// <typeparam name="TAggregateRoot"></typeparam>
    public abstract class EFPaginationRepository<TDbContext, TAggregateRoot> : EFPaginationRepository<TDbContext, TAggregateRoot, int>, IPaginationRepository<TAggregateRoot, int>
            where TAggregateRoot : class, IAggregateRoot<int>
            where TDbContext : DbContext
    {
        protected EFPaginationRepository(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}
