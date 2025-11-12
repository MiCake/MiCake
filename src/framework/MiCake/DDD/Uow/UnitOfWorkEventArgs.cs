using System;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Event arguments for Unit of Work lifecycle events
    /// </summary>
    public class UnitOfWorkEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the unit of work
        /// </summary>
        public Guid UnitOfWorkId { get; }
        
        /// <summary>
        /// Whether the unit of work is nested
        /// </summary>
        public bool IsNested { get; }
        
        /// <summary>
        /// Optional exception if the event is triggered by an error
        /// </summary>
        public Exception? Exception { get; }
        
        public UnitOfWorkEventArgs(Guid unitOfWorkId, bool isNested, Exception? exception = null)
        {
            UnitOfWorkId = unitOfWorkId;
            IsNested = isNested;
            Exception = exception;
        }
    }
}
