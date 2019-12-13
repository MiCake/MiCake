using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow.Easy
{
    public interface IUnitOfWork : IDisposable, ITranscationFeatureContainer
    {
        Guid ID { get; }

        void SaveChanges();

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        void Rollback();

        Task RollbackAsync(CancellationToken cancellationToken = default);

    }
}
