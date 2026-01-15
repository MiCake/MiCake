using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates.Events
{
    /// <summary>
    /// Domain event raised when a book's author information is changed.
    /// </summary>
    /// <remarks>
    /// This domain event demonstrates the DDD event pattern where domain state changes
    /// can trigger side effects handled by domain event handlers.
    /// </remarks>
    public class BookChangeEvent : DomainEvent
    {
        /// <summary>
        /// Gets the name of the book that was changed.
        /// </summary>
        public string BookName { get; }

        /// <summary>
        /// Gets the timestamp when the change occurred.
        /// </summary>
        public DateTime ChangedAt { get; }

        /// <summary>
        /// Creates a new BookChangeEvent.
        /// </summary>
        /// <param name="bookName">The name of the book</param>
        public BookChangeEvent(string bookName)
        {
            BookName = bookName;
            ChangedAt = DateTime.UtcNow;
        }
    }
}
