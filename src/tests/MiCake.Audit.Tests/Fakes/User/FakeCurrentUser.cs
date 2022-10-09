using MiCake.Identity;
using System;

namespace MiCake.Audit.Tests.Fakes.User
{
    public class FakeCurrentUser_Guid : CurrentMiCakeUser<Guid>
    {
        public override Guid GetUserID()
        {
            return Guid.NewGuid();
        }
    }

    public class FakeCurrentUser_long : CurrentMiCakeUser<long>
    {
        public override long GetUserID()
        {
            return 1001;
        }
    }
}
