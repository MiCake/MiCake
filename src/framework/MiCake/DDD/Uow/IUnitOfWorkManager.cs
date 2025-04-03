using MiCake.DDD.Uow.Internal;
using System;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// A <see cref="UnitOfWork"/> manager.
    /// Responsible for creating and managing work <see cref="UnitOfWork"/>
    /// </summary>
    public interface IUnitOfWorkManager : IDisposable
    {
        /// <summary>
        /// Get units of work in the current scope.
        /// </summary>
        IUnitOfWork GetCurrentUnitOfWork();

        /// <summary>
        /// Get <see cref="UnitOfWork"/> by unit of work id.
        /// </summary>
        /// <param name="Id">the unit of work id</param>
        IUnitOfWork GetUnitOfWork(Guid Id);

        /// <summary>
        /// Create a <see cref="IUnitOfWork"/> with a default options.
        /// </summary>
        IUnitOfWork Create();

        /// <summary>
        /// Create a <see cref="IUnitOfWork"/> with a unit of work scope.
        /// </summary>
        /// <param name="unitOfWorkScope"><see cref="UnitOfWorkScope"/></param>
        IUnitOfWork Create(UnitOfWorkScope unitOfWorkScope);

        /// <summary>
        ///  Create a <see cref="IUnitOfWork"/> with a custom options.
        /// </summary>
        /// <param name="options"><see cref="UnitOfWorkOptions"/></param>
        IUnitOfWork Create(UnitOfWorkOptions options);
    }
}
