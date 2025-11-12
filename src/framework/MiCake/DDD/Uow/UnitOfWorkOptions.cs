using System.Data;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Options for configuring unit of work behavior
    /// </summary>
    public class UnitOfWorkOptions
    {
        /// <summary>
        /// Transaction isolation level. Default is ReadCommitted.
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; } = System.Data.IsolationLevel.ReadCommitted;

        /// <summary>
        /// Whether to automatically begin a transaction when UoW is created.
        /// Default is true.
        /// </summary>
        public bool AutoBeginTransaction { get; set; } = true;

        /// <summary>
        /// Timeout for the transaction in seconds. Null means no timeout.
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// Whether this is a read-only unit of work (optimization for queries).
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// Creates default options
        /// </summary>
        public static UnitOfWorkOptions Default => new();

        /// <summary>
        /// Creates read-only options (no transaction, optimized for queries)
        /// </summary>
        public static UnitOfWorkOptions ReadOnly => new()
        {
            AutoBeginTransaction = false,
            IsReadOnly = true
        };
    }
}
