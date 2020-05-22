using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Uow
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// </summary>
    public class UnitOfWorkNeedParts
    {
        public UnitOfWorkOptions Options { get; set; }

        public IServiceScope ServiceScope { get; set; }

        public UnitOfWorkNeedParts()
        {
        }
    }
}
