using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    public interface IUnitOfWorkProvider
    {
        /// <summary>
        /// If there is a work unit with tree nesting relationship
        ///Then the work unit of the last leaf node is returned
        /// </summary>
        /// <returns></returns>
        IUnitOfWork GetCurrentUnitOfWork();

        IUnitOfWork GetUnitOfWork(Guid Id);
    }
}
