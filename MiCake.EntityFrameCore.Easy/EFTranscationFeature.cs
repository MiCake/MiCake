using MiCake.Uow.Easy;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameCore.Easy
{
    public class EFTranscationFeature : ITranscationFeature
    {
        private readonly DbContext _dbContext;

        public EFTranscationFeature(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Commit()
        {
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Rollback()
        {
            throw new NotImplementedException();
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
