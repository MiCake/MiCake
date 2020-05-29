using MiCake.Core.Handlers;
using System.Threading;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Handlers
{
    public class DemoExceptionHanlder : IMiCakeExceptionHandler
    {
        public Task Handle(MiCakeExceptionContext exceptionContext, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
