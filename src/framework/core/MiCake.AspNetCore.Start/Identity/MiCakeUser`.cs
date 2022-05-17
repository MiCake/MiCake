using MiCake.DDD.Domain;
using MiCake.Identity;
using MiCake.Identity.Authentication;
using MiCake.Identity.Authentication.JwtToken;

namespace MiCake.AspNetCore.Identity
{
    /// <summary>
    /// Represents a user in the miacke identity system
    /// </summary>
    /// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
    public abstract class MiCakeUser<TKey> : AggregateRoot<TKey>, IMiCakeUser<TKey> where TKey : notnull
    {
        [JwtClaim(ClaimName = MiCakeClaimTypes.UserId)]
        protected TKey IdForClaim => this.Id;
    }
}
