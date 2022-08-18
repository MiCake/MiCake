using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.Identity;

namespace MiCake.Audit.Tests.Fakes.User
{
    public class HasAuditUser : Entity<long>, IMiCakeUser<long>, IHasCreatedUser<long>, IHasUpdatedUser<long>, IHasDeletedUser<long>, ISoftDeletion
    {


        public HasAuditUser()
        {
        }

        public long? CreatedBy { get; set; }

        public long? UpdatedBy { get; set; }

        public long? DeletedBy { get; set; }

        public bool IsDeleted { get; set; }
    }

    public class HasAuditUserWithNoSoftDeletion : Entity<long>, IMiCakeUser<long>, IHasCreatedUser<long>, IHasUpdatedUser<long>, IHasDeletedUser<long>
    {
        public long? CreatedBy { get; set; }
        public long? UpdatedBy { get; set; }
        public long? DeletedBy { get; set; }

        public HasAuditUserWithNoSoftDeletion()
        {
        }
    }
}
