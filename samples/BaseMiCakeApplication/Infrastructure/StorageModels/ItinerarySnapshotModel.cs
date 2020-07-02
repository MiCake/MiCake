using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Extensions.Store;
using System;

namespace BaseMiCakeApplication.Infrastructure.StroageModels
{
    public class ItinerarySnapshotModel : PersistentObject<Itinerary, ItinerarySnapshotModel>, IHasAuditWithSoftDeletion
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime NoteTime { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime? ModificationTime { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletionTime { get; set; }

        public override void ConfigureMapping()
        {
            MapConfig.MapProperty(d => d.Note.Content, s => s.Content)
                          .MapProperty(d => d.Note.NoteTime, s => s.NoteTime)
                          .MapProperty(d => d.Id, s => s.Id);
        }
    }
}
