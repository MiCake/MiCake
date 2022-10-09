using System.Data;

namespace MiCake.Uow
{
    public class UnitOfWorkOptions
    {
        /// <summary>
        /// Specifies the transaction locking behavior for the connection.
        /// <see cref="IsolationLevel"/>
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; }

        /// <summary>
        /// Time out config.
        /// </summary>
        public TimeSpan? Timeout { get; set; }
    }
}
