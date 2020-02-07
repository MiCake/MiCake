using BaseMiCakeApplication.Infrastructure.StroageModel;
using MiCake.Audit;
using MiCake.DDD.Domain;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    public class Itinerary : HasSnapshotAggregateRoot<Guid, ItinerarySnapshotModel>,IHasCreationTime
    {
        public ItineraryNote Note { get; private set; }
        public DateTime CreationTime { get; set; }

        //must have this ctor
        public Itinerary(ItinerarySnapshotModel snapshot) : base(snapshot)
        {
            Note = new ItineraryNote(snapshot.Content);
            Id = snapshot.ID;
        }

        public override ItinerarySnapshotModel GetSnapshot()
        {
            return new ItinerarySnapshotModel()
            {
                Content = Note.Content,
                ID = Id,
                NoteTime = Note.NoteTime
            };
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
        }
    }
}
