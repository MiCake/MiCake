using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Core.Handlers
{
    /// <summary>
    /// Intercepting errors (<see cref="MiCakeException"/>).
    /// </summary>
    public interface IMiCakeExceptionHandler
    {
        /// <summary>
        /// Handle micake exception
        /// </summary>
        /// <param name="exceptionContext"><see cref="MiCakeExceptionContext"/></param>
        /// <param name="cancellationToken"></param>
        Task Handle(MiCakeExceptionContext exceptionContext, CancellationToken cancellationToken = default);
    }
}