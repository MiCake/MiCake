using BaseMiCakeApplication.Domain.Aggregates;
using Mapster;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Extensions.Store;
using System;

namespace BaseMiCakeApplication.Infrastructure.StroageModels
{
    public class ItinerarySnapshotModel : PersistentObject<Itinerary>, IHasAuditWithSoftDeletion
    {
        public Guid ID { get; set; }
        public string Content { get; set; }
        public DateTime NoteTime { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime? ModificationTime { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletionTime { get; set; }

        public override void ConfigureMapping()
        {
            TypeAdapterConfig<Itinerary, ItinerarySnapshotModel>.NewConfig()
                .MapDomainEvent()
                .TwoWays()
                .Map(s => s.Content, d => d.Note.Content)
                .Map(s => s.NoteTime, d => d.Note.NoteTime)
                .Map(s => s.ID, d => d.Id);
        }
    }
}
