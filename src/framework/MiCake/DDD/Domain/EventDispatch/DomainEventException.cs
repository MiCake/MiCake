using System;

namespace MiCake.DDD.Domain.EventDispatch
{
    /// <summary>
    /// Exception thrown when a domain event fails to dispatch.
    /// </summary>
    public class DomainEventException : Exception
    {
        /// <summary>
        /// Gets the domain event that failed to dispatch.
        /// </summary>
        public IDomainEvent? FailedEvent { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEventException"/> class.
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="failedEvent">The domain event that failed</param>
        /// <param name="innerException">The underlying exception</param>
        public DomainEventException(string message, IDomainEvent? failedEvent = null, Exception? innerException = null)
            : base(message, innerException)
        {
            FailedEvent = failedEvent;
        }
    }
}
