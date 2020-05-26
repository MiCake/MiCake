using System;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    /// <summary>
    /// Specifies events which the <see cref="IUnitOfWork"/> invokes to enable developer control unit of work process.
    /// </summary>
    public class UnitOfWorkEvents
    {
        /// <summary>
        /// Invoked if unit of work has completed.it is mean all transaction has commit.
        /// </summary>
        public Func<IUnitOfWork, Task> OnCompleted { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Invoked if there is a commit error in the unit of work, it is called after the unit of work rollback.
        /// </summary>
        public Func<IUnitOfWork, Task> OnRollbacked { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Invoked when <see cref="UnitOfWork "/> dispose.
        /// </summary>
        public Action<IUnitOfWork> OnDispose { get; set; } = context => { };

        public virtual Task Completed(IUnitOfWork context) => OnCompleted?.Invoke(context);

        public virtual Task Rollbacked(IUnitOfWork context) => OnRollbacked?.Invoke(context);

        public virtual void Dispose(IUnitOfWork context) => OnDispose?.Invoke(context);
    }
}
