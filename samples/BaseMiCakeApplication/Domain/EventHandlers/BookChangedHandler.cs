using BaseMiCakeApplication.Domain.Aggregates.Events;
using BaseMiCakeApplication.Domain.Repositories;
using MiCake.DDD.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Domain.EventHandlers
{
    /// <summary>
    /// Handler for BookChangeEvent domain events.
    /// </summary>
    /// <remarks>
    /// This handler demonstrates how to handle domain events raised by aggregates.
    /// Domain events provide a way to decouple domain logic from side effects.
    /// </remarks>
    public class BookChangedHandler : IDomainEventHandler<BookChangeEvent>
    {
        private readonly IBookRepository _bookRepository;
        private readonly ILogger<BookChangedHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the BookChangedHandler.
        /// </summary>
        /// <param name="bookRepository">The book repository</param>
        /// <param name="logger">The logger</param>
        public BookChangedHandler(IBookRepository bookRepository, ILogger<BookChangedHandler> logger)
        {
            _bookRepository = bookRepository;
            _logger = logger;
        }

        /// <summary>
        /// Handles the BookChangeEvent asynchronously.
        /// </summary>
        /// <remarks>
        /// In a real-world scenario, this could:
        /// 1. Update related entities
        /// 2. Trigger other domain operations
        /// 3. Update caches
        /// 4. Send notifications
        /// </remarks>
        public async Task HandleAsync(BookChangeEvent domainEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Book '{domainEvent.BookName}' author was changed at {domainEvent.ChangedAt}");

            // Example: You could perform follow-up operations here
            // such as logging the change, updating audit tables, or triggering other processes
            
            await Task.CompletedTask;
        }
    }
}
