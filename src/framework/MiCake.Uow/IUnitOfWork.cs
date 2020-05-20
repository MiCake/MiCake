using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    /// <summary>
    /// Define a unit of work.
    /// </summary>
    public interface IUnitOfWork : IDisposable, ITransactionFeatureContainer
    {
        Guid ID { get; }

        bool IsDisposed { get; }

        UnitOfWorkOptions UnitOfWorkOptions { get; }

        /// <summary>
        /// A unit of work scoped serviceprovider.
        /// Can get db instance or transaction instance in this scope.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        void SetOptions(UnitOfWorkOptions options);

        void SaveChanges();

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        void Rollback();

        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
