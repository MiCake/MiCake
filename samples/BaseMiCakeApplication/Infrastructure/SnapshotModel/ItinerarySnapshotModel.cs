using MiCake.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Infrastructure.StroageModel
{
    public class ItinerarySnapshotModel:IHasCreationTime
    {
        public Guid ID { get; set; }
        public string Content { get; set; }
        public DateTime NoteTime { get; set; }
        public DateTime CreationTime { get ; set ; }
    }
}
