using MiCake.Audit;
using MiCake.DDD.Domain;
using System;

namespace MiCake.Identity.Tests.FakeUser
{
    /// <summary>
    /// Will be worng,creator key type is different from user key type.
    /// </summary>
    public class HasAuditUserWithWrongKeyType : Entity<long>, IMiCakeUser<long>, IHasCreator<Guid>
    {
        public Guid CreatorID { get; set; }

        public HasAuditUserWithWrongKeyType()
        {
        }
    }
}
