using MiCake.Audit;

namespace MiCake.Identity.Tests.FakeUser
{
    public class HasAuditUser : IMiCakeUser<long>, IHasCreator<long>, IHasModifyUser<long>, IHasDeleteUser<long>
    {
        public long Id { get; set; }
        public long CreatorID { get; set; }
        public long ModifyUserID { get; set; }
        public long DeleteUserID { get; set; }

        public HasAuditUser()
        {
        }
    }
}
