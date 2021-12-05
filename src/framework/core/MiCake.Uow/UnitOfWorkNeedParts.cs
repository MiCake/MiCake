using Microsoft.Extensions.DependencyInjection;
using System;

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

        /// <summary>
        /// Use to dispose current unit of work and ensure the order in the stack is accurate
        /// </summary>
        public Action<IUnitOfWork> DisposeHandler { get; set; }

        public UnitOfWorkNeedParts()
        {
        }
    }
}
