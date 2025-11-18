namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Defines the persistence strategy for how data changes are persisted to the database.
    /// This determines whether explicit database transactions are required or if implicit transaction handling is sufficient.
    /// </summary>
    public enum PersistenceStrategy
    {
        /// <summary>
        /// Transaction Managed Strategy - Requires explicit database transaction control.
        /// Use this when you need full ACID guarantees, multiple database operations must be atomic,
        /// or you need savepoint support for complex transaction scenarios.
        /// </summary>
        TransactionManaged = 0,

        /// <summary>
        /// Optimize For Single Write Strategy - Optimized for single write operations.
        /// Use this when the Unit of Work contains simple, single operations that don't require explicit transaction management.
        /// <para>
        /// If you are using EF Core as your persistence provider, this strategy will use EF Core's SaveChangesAsync method.
        /// Be careful: If you call SaveChangesAsync before the unit of work is committed, the data you committed may not be rolled back when the unit of work fails.
        /// </para>
        /// <para>
        /// If you want to ensure data consistency across multiple operations, consider using the <see cref="TransactionManaged"/> strategy instead.
        /// </para>
        /// </summary>
        OptimizeForSingleWrite = 1
    }
}
