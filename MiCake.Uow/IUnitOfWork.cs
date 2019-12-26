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

        void SaveChanges();

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        void Rollback();

        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
