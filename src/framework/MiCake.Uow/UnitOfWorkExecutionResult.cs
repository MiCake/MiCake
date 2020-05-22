using System;

namespace MiCake.Uow
{
    /// <summary>
    /// Execution result of database operation.
    /// <see cref="IUnitOfWorkExecutor"/>
    /// </summary>
    public class UnitOfWorkExecutionResult
    {
        /// <summary>
        /// <see cref="UnitOfWorkResultType"/>
        /// </summary>
        public UnitOfWorkResultType Result { get; protected set; }

        /// <summary>
        ///  Holds failure information from the execution.
        /// </summary>
        public Exception Failure { get; protected set; }


        public UnitOfWorkExecutionResult()
        {
        }

        /// <summary>
        /// Indicates that database operation was successful.
        /// </summary>
        public static UnitOfWorkExecutionResult Success()
            => new UnitOfWorkExecutionResult() { Result = UnitOfWorkResultType.Succeeded };

        /// <summary>
        /// Indicates that there was a failure during database operation.
        /// </summary>
        public static UnitOfWorkExecutionResult Fail(string exceptionMessage)
            => new UnitOfWorkExecutionResult() { Result = UnitOfWorkResultType.Failed, Failure = new Exception(exceptionMessage) };

        /// <summary>
        /// Indicates that there was a failure during database operation.
        /// </summary>
        public static UnitOfWorkExecutionResult Success(Exception failure)
            => new UnitOfWorkExecutionResult() { Result = UnitOfWorkResultType.Failed, Failure = failure };
    }

    public enum UnitOfWorkResultType
    {
        /// <summary>
        /// Indicates successful.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Indicates failure.
        /// </summary>
        Failed,
    }
}
