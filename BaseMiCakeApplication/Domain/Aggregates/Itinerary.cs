using BaseMiCakeApplication.Infrastructure.StroageModel;
using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    public class Itinerary : AggregateRoot<Guid>, IEntityHasSnapshot<ItinerarySnapshotModel>
    {
        public ItineraryNote Note { get; private set; }

        //must have this ctor
        public Itinerary(ItinerarySnapshotModel snapshot)
        {
            Note = new ItineraryNote(snapshot.Content);
            Id = snapshot.ID;
        }

        public ItinerarySnapshotModel GetSnapshot()
        {
            return new ItinerarySnapshotModel()
            {
                Content = Note.Content,
                ID = Id,
                NoteTime = Note.NoteTime
            };
        }

        //ctor
        public Itinerary()
        {
        }

        public void ChangeNote(string content)
        {
            Note = new ItineraryNote(content);
        }

        
    }
}
