using MiCake.Identity;
using MiCake.Identity.Authentication;
using System;

namespace BaseMiCakeApplication.Domain.Aggregates
{
    public class User : IMiCakeUser<Guid>
    {
        [JwtClaim]
        public Guid Id { get; set; }

        [JwtClaim]
        public string Name { get; set; }

        public User()
        {
            Id = Guid.NewGuid();
        }

        public void SetName(string name)
        {
            Name = name;
        }
    }
}
