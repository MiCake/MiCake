using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Uow
{
    /// <summary>
    /// Execute database operation, which will be submitted by the outer transaction after completion.
    /// It will maintain a reference to the database operand.For example:DbContext in EFCore.
    /// </summary>
    public interface IUnitOfWorkExecutor : IDisposable
    {
        /// <summary>
        /// Indicate this executor has executed.
        /// </summary>
        public bool HasExecuted { get; }

        /// <summary>
        /// Judge whether the current operation can be performed according to the current environment
        /// </summary>
        /// <param name="context"></param>
        /// <returns>If true,will invoke <see cref="Execute"/></returns>
        public bool CanExecute(UnitOfWorkExecutionContext context);

        /// <summary>
        /// Execute database operation.
        /// </summary>
        public UnitOfWorkExecutionResult Execute();

        /// <summary>
        /// Execute database operation.
        /// </summary>
        public Task<UnitOfWorkExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
