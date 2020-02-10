using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates.Events
{
    public class ItineraryNoteChanged : DomainEvent
    {
        public Guid ItineraryID { get; set; }

        public ItineraryNoteChanged(Guid Id)
        {
            ItineraryID = Id;
        }
    }
}
