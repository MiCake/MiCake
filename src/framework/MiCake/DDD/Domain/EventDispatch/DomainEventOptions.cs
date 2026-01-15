namespace MiCake.DDD.Domain.EventDispatch
{
    /// <summary>
    /// Configuration options for domain event dispatching behavior.
    /// </summary>
    public class DomainEventOptions
    {
        /// <summary>
        /// Defines how to handle failures when dispatching domain events.
        /// </summary>
        public enum EventFailureStrategy
        {
            /// <summary>
            /// Continue processing remaining events even if one fails. 
            /// Failed events are logged but do not stop execution.
            /// </summary>
            ContinueOnError,

            /// <summary>
            /// Stop processing remaining events if one fails.
            /// Failed events are logged and no further events are dispatched.
            /// </summary>
            StopOnError,

            /// <summary>
            /// Throw an exception when an event fails.
            /// This will cause the entire operation to fail and rollback.
            /// Recommended for maintaining data consistency.
            /// </summary>
            ThrowOnError
        }

        /// <summary>
        /// Gets or sets the failure handling strategy for domain events.
        /// Default: <see cref="EventFailureStrategy.ThrowOnError"/>
        /// </summary>
        public EventFailureStrategy OnEventFailure { get; set; } = EventFailureStrategy.ThrowOnError;
    }
}
