using System;
using System.Threading.Tasks;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Specifies events which the <see cref="IUnitOfWork"/> invokes to enable developer control unit of work process.
    /// </summary>
    public class UnitOfWorkEvents
    {
        /// <summary>
        /// Invoked if unit of work has completed.it is mean all transaction has commit.
        /// Be careful about concurrency, because none of these events will be await
        /// </summary>
        public Func<IUnitOfWork, Task> OnCompleted { get; set; } = context => Task.CompletedTask;

        /// <summary>
        /// Invoked if there is a commit error in the unit of work, it is called after the unit of work rollback.
        /// Be careful about concurrency, because none of these events will be await
        /// </summary>
        public Func<IUnitOfWork, Task> OnRollbacked { get; set; } = context => Task.CompletedTask;

        public virtual Task Completed(IUnitOfWork context) => OnCompleted?.Invoke(context);

        public virtual Task Rollbacked(IUnitOfWork context) => OnRollbacked?.Invoke(context);
    }
}
