using BaseMiCakeApplication.Domain.Events;
using BaseMiCakeApplication.Domain.Repositories;
using MiCake.DDD.Domain;
using MiCake.Uow;
using System.Threading;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Domain.EventHandlers
{
    public class NewBookChangedHandler : IDomainEventHandler<NewBookChangeEvent>
    {
        private readonly IBookRepository _repo;
        private readonly IUnitOfWorkManager _uow;

        public NewBookChangedHandler(IBookRepository repo, IUnitOfWorkManager uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task HandleAysnc(NewBookChangeEvent domainEvent, CancellationToken cancellationToken = default)
        {
            await _repo.AddAsync(new Aggregates.Book("xx", "x", "x1"));
            await _repo.AddAsync(new Aggregates.Book("xx", "x", "x1"));
            await _repo.AddAsync(new Aggregates.Book("xx", "x", "x1"));
        }
    }
}
