using BaseMiCakeApplication.Domain.Aggregates.Events;
using MiCake.DDD.Domain.Store;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    public class Itinerary : AggregateRootHasPersistentObject<Guid>
    {
        public ItineraryNote Note { get; private set; }

        public Itinerary()
        {
        }

        //ctor
        public Itinerary(string content)
        {
            Id = Guid.NewGuid();
            Note = new ItineraryNote(content);
        }

        public void ChangeNote(string content)
        {
            Note = new ItineraryNote(content);

            AddDomainEvent(new ItineraryNoteChanged(Id));
        }
    }
}
