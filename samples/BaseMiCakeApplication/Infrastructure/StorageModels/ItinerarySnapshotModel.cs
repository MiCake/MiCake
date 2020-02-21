using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.Audit;
using MiCake.DDD.Extensions.Store;
using System;

namespace BaseMiCakeApplication.Infrastructure.StroageModels
{
    public class ItinerarySnapshotModel : StorageModel<Itinerary>, IHasCreationTime
    {
        public Guid ID { get; set; }
        public string Content { get; set; }
        public DateTime NoteTime { get; set; }
        public DateTime CreationTime { get; set; }

        public override void ConfigureMapping()
        {
        }
    }
}
