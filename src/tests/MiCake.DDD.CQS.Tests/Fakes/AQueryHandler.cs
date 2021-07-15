using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.DDD.CQS.Tests.Fakes
{
    public class AQueryHandler : IQueryHandler<AQueryModel, string>, IQueryHandler<BQueryModel, string>
    {
        public Task<string> Handle(AQueryModel queryModel, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> Handle(BQueryModel queryModel, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
