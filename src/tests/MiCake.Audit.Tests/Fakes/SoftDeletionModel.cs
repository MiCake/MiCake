using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;

namespace MiCake.Audit.Tests.Fakes
{
    public class SoftDeletionModel : Entity, ISoftDeletion
    {
        public bool IsDeleted { get; set; }
    }
}
