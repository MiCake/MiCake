using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiCake.Aop.Tests.InterfaceProxies
{
    public interface IInterfaceToProxy
    {
        IReadOnlyList<string> Log { get; }

        void SynchronousVoidMethod();

        void SynchronousVoidExceptionMethod();

        Guid SynchronousResultMethod();

        Guid SynchronousResultExceptionMethod();

        Task AsynchronousVoidMethod();

        Task AsynchronousVoidExceptionMethod();

        Task<Guid> AsynchronousResultMethod();

        Task<Guid> AsynchronousResultExceptionMethod();
    }
}
