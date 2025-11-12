using System.Data;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Defines when transactions should be initialized
    /// </summary>
    public enum TransactionInitializationMode
    {
        /// <summary>
        /// Lazy initialization - Transaction is started when first resource (DbContext) is accessed.
        /// This is the default mode and provides better performance for operations that may not need transactions.
        /// </summary>
        Lazy = 0,

        /// <summary>
        /// Immediate initialization - Transaction is started immediately when UoW is created.
        /// Use this when you need guaranteed transaction semantics from the start.
        /// Note: Requires registered DbContext types to be specified via IUnitOfWorkManager extensions.
        /// </summary>
        Immediate = 1
    }

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
        /// Determines when transactions should be initialized.
        /// Default is Lazy (transactions start when first resource is accessed).
        /// Set to Immediate to start transactions as soon as UoW is created.
        /// </summary>
        public TransactionInitializationMode InitializationMode { get; set; } = TransactionInitializationMode.Lazy;

        /// <summary>
        /// Timeout for the transaction in seconds. Null means no timeout.
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// Whether this is a read-only unit of work (optimization for queries).
        /// When true, transactions will not be started.
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// Creates default options with lazy initialization
        /// </summary>
        public static UnitOfWorkOptions Default => new();

        /// <summary>
        /// Creates options with immediate transaction initialization
        /// </summary>
        public static UnitOfWorkOptions Immediate => new()
        {
            InitializationMode = TransactionInitializationMode.Immediate
        };

        /// <summary>
        /// Creates read-only options (no transaction, optimized for queries)
        /// </summary>
        public static UnitOfWorkOptions ReadOnly => new()
        {
            IsReadOnly = true
        };
    }
}
