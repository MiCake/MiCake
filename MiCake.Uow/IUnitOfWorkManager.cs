using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Uow
{
    public interface IUnitOfWorkManager : IUnitOfWorkProvider
    {
        /// <summary>
        /// Create a <see cref="IUnitOfWork"/> with a default options
        /// </summary>
        IUnitOfWork Create();

        /// <summary>
        ///  Create a <see cref="IUnitOfWork"/> with a custom options
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        IUnitOfWork Create(UnitOfWorkOptions options);
    }
}
