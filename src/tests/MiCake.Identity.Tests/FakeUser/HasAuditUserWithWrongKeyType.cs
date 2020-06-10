using MiCake.Audit;
using System;

namespace MiCake.Identity.Tests.FakeUser
{
    /// <summary>
    /// Will be worng,creator key type is different from user key type.
    /// </summary>
    public class HasAuditUserWithWrongKeyType : IMiCakeUser<long>, IHasCreator<Guid>
    {
        public Guid CreatorID { get; set; }
        public long Id { get; set; }

        public HasAuditUserWithWrongKeyType()
        {
        }
    }
}
