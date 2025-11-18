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
        /// Defines the persistence strategy for how data changes are persisted.
        /// </summary>
        public PersistenceStrategy Strategy { get; set; } = PersistenceStrategy.OptimizeForSingleWrite;

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
        /// Creates default options with OptimizeForSingleWrite strategy and lazy initialization.
        /// This is the recommended default for most operations.
        /// </summary>
        public static UnitOfWorkOptions Default => new()
        {
            Strategy = PersistenceStrategy.OptimizeForSingleWrite,
            InitializationMode = TransactionInitializationMode.Lazy
        };

        /// <summary>
        /// Creates options with TransactionManaged strategy and immediate transaction initialization.
        /// Use this for complex multi-operation scenarios requiring explicit transaction control.
        /// </summary>
        public static UnitOfWorkOptions Immediate => new()
        {
            Strategy = PersistenceStrategy.TransactionManaged,
            InitializationMode = TransactionInitializationMode.Immediate
        };

        /// <summary>
        /// Creates read-only options (no transaction, optimized for queries).
        /// Useful for read-only operations that don't require transaction protection.
        /// </summary>
        public static UnitOfWorkOptions ReadOnly => new()
        {
            IsReadOnly = true
        };
    }
}
