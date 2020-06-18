using MiCake.AspNetCore.Security;
using MiCake.Core.Util.Converts;
using MiCake.Identity;
using Microsoft.AspNetCore.Http;
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

            var userId = ConvertHelper.ConvertValue<string, TKey>(userIDClaim.Value);
            return userId;
        }
    }
}
