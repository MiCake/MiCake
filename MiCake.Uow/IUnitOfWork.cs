using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    public interface IUnitOfWork : IDisposable, ITransactionFeatureContainer
    {
        Guid ID { get; }

        bool IsDisposed { get; }

        UnitOfWorkOptions UnitOfWorkOptions { get; }

        /// <summary>
        /// a unit of work scoped serviceprovider.
        /// can get db instance or transaction instance in this scope.
        /// for example:in ef core.can get a dbcontext with uow scope.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        void SetOptions(UnitOfWorkOptions options);

        void SaveChanges();

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        void Rollback();

        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
