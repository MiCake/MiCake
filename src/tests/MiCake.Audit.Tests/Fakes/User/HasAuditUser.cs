using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.Identity;

namespace MiCake.Audit.Tests.Fakes.User
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
