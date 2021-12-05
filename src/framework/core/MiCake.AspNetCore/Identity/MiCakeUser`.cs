using MiCake.DDD.Domain;
using MiCake.Identity;

namespace MiCake.AspNetCore.Identity
{
    /// <summary>
    /// Represents a user in the miacke identity system
    /// </summary>
    /// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
    public abstract class MiCakeUser<TKey> : AggregateRoot<TKey>, IMiCakeUser<TKey>
    {
    }
}
