using BaseMiCakeApplication.Domain.Aggregates.Events;
using MiCake.DDD.Domain;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Domain.EventHandlers
{
    public class ItineraryNoteChangedHandler : IDomainEventHandler<ItineraryNoteChanged>
    {
        public Task HandleAysnc(ItineraryNoteChanged domainEvent, CancellationToken cancellationToken = default)
        {
            Debug.Write($"exec {nameof(ItineraryNoteChangedHandler)}");
            return Task.CompletedTask;
        }
    }
}
