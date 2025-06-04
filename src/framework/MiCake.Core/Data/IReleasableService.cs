using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Core.Data
{
    /// <summary>
    /// Define a service that can release resources.
    /// </summary>
    public interface IReleasableService
    {
        /// <summary>
        /// Release the resources occupied by the service.
        /// </summary>
        void Release();

        /// <summary>
        /// Release the resources occupied by the service.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous operation. </returns>
        Task ReleaseAsync(CancellationToken cancellationToken = default);
    }
}
