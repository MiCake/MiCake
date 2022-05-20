using MiCake.Core.Data;
using MiCake.Identity.Authentication;

namespace MiCake.AspNetCore.Identity
{
    /// <summary>
    /// The options for config micake identity.
    /// </summary>
    public class MiCakeIdentityOptions : ICanApplyData<MiCakeIdentityOptions>
    {
        /// <summary>
        /// Specify the user ID claim name to get value from HttpContext.User.Claims. Default value is : <see cref="MiCakeClaimTypes.UserId"/>.
        /// </summary>
        public string UserIdClaimName { get; set; } = MiCakeClaimTypes.UserId;

        public void Apply(MiCakeIdentityOptions data)
        {
            UserIdClaimName = data.UserIdClaimName;
        }
    }
}
