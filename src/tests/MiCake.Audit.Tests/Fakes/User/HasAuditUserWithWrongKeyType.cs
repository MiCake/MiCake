using MiCake.DDD.Domain;
using MiCake.Identity;
using System;

namespace MiCake.Audit.Tests.Fakes.User
{
    /// <summary>
    /// Will be worng,creator key type is different from user key type.
    /// </summary>
    public class HasAuditUserWithWrongKeyType : Entity<long>, IMiCakeUser<long>, IHasCreatedUser<Guid>
    {
        public Guid? CreatedBy { get; protected set; }

        public HasAuditUserWithWrongKeyType()
        {
        }
    }
}
