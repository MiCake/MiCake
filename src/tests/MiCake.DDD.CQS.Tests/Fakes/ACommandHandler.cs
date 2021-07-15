using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.CQS.Tests.Fakes
{
    class ACommandHandler : ICommandHandler<ACommand>
    {
        public Task Handle(ACommand command, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
