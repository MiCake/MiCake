using MiCake.Identity.Authentication.JwtToken;
using System;

namespace MiCake.Identity.Tests.FakeUser
{
    public class UserWithJwtClaim : IMiCakeUser<Guid>
    {
        [JwtClaim(ClaimName = "userid")]
        public Guid Id { get; set; }

        [JwtClaim()]
        public string Name { get; set; }

        public UserWithJwtClaim()
        {
        }
    }
}
