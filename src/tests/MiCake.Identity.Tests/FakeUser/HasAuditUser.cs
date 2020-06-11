using MiCake.Audit;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;

namespace MiCake.Identity.Tests.FakeUser
{
    public class HasAuditUser : Entity<long>, IMiCakeUser<long>, IHasCreator<long>, IHasModifyUser<long>, IHasDeleteUser<long>, ISoftDeletion
    {
        public long CreatorID { get; set; }
        public long ModifyUserID { get; set; }
        public long DeleteUserID { get; set; }
        public bool IsDeleted { get; set; }

        public HasAuditUser()
        {
        }
    }

    public class HasAuditUserWithNoSoftDeletion : Entity<long>, IMiCakeUser<long>, IHasCreator<long>, IHasModifyUser<long>, IHasDeleteUser<long>
    {
        public long CreatorID { get; set; }
        public long ModifyUserID { get; set; }
        public long DeleteUserID { get; set; }

        public HasAuditUserWithNoSoftDeletion()
        {
        }
    }
}
