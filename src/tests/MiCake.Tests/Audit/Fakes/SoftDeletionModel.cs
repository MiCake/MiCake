using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;

namespace MiCake.Audit.Tests.Fakes
{
    public class SoftDeletionModel : Entity, ISoftDeletable
    {
        public bool IsDeleted { get; set; }
    }
}
