using BaseMiCakeApplication.Domain.Aggregates.Events;
using BaseMiCakeApplication.Domain.Repositories;
using MiCake.DDD.Domain;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Domain.EventHandlers
{
    public class BookChangedHandler : IDomainEventHandler<BookChangeEvent>
    {
        private readonly IBookRepository _repo;

        public BookChangedHandler(IBookRepository repo)
        {
            _repo = repo;
        }

        public async Task HandleAysnc(BookChangeEvent domainEvent, CancellationToken cancellationToken = default)
        {
            await _repo.AddAsync(new Aggregates.Book("xx", "x", "x1"), cancellationToken);
            await _repo.AddAsync(new Aggregates.Book("xx", "x", "x1"), cancellationToken);
            await _repo.AddAsync(new Aggregates.Book("xx", "x", "x1"), cancellationToken);
        }
    }
}
