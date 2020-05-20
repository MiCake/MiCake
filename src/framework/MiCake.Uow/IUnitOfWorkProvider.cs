using System;

namespace MiCake.Uow
{
    /// <summary>
    /// A provider for get <see cref="IUnitOfWork"/>
    /// </summary>
    public interface IUnitOfWorkProvider
    {
        /// <summary>
        /// Get units of work in the current scope
        /// </summary>
        IUnitOfWork GetCurrentUnitOfWork();

        /// <summary>
        /// Get <see cref="UnitOfWork"/> by unit of work id.
        /// </summary>
        /// <param name="Id">the unit of work id</param>
        IUnitOfWork GetUnitOfWork(Guid Id);
    }
}
