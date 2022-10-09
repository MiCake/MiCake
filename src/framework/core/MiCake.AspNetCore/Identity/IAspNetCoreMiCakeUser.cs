using System.Security.Claims;

namespace MiCake.AspNetCore.Identity
{
    /// <summary>
    /// Get current http request user claim info.
    /// </summary>
    public interface IAspNetCoreMiCakeUser
    {
        public ClaimsPrincipal? User { get; }
    }
}
