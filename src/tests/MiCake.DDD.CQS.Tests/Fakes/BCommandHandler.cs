using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.CQS.Tests.Fakes
{
    class BCommandHandler : ICommandHandler<BCommand, string>
    {
        public Task<string> Handle(BCommand command, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        Task ICommandHandler<BCommand>.Handle(BCommand command, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
