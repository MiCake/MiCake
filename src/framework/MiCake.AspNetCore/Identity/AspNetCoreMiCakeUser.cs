using MiCake.AspNetCore.Security;
using MiCake.Identity;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;

namespace MiCake.AspNetCore.Identity
{
    internal class AspNetCoreMiCakeUser<TKey> : CurrentMiCakeUser<TKey>, IAspNetCoreMiCakeUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimsPrincipal User => _httpContextAccessor.HttpContext.User;

        public AspNetCoreMiCakeUser(IHttpContextAccessor httpContextAccessor) : base()
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override TKey GetUserID()
        {
            var userIDClaim = _httpContextAccessor.HttpContext.User?.Claims
                                                  .FirstOrDefault(s => s.Type.Equals(VerifyUserClaims.UserID));

            if (userIDClaim == null)
                return default;

            //convert string to TKey type.
            var userId = (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(userIDClaim.Value);
            return userId;
        }
    }
}
