using MiCake.AspNetCore.Security;
using MiCake.Core.Util.Convert;
using MiCake.Identity;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace MiCake.AspNetCore.Identity
{
    internal class AspNetCoreMiCakeUser<TKey> : CurrentMiCakeUser<TKey>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AspNetCoreMiCakeUser(IHttpContextAccessor httpContextAccessor) : base()
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override TKey GetUserID()
        {
            var userIDClaim = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(s => s.Type.Equals(VerifyUserClaims.UserID));

            if (userIDClaim == null)
                return default;

            var userId = ConvertHelper.ConvertValue<string, TKey>(userIDClaim.Value);
            return userId;
        }
    }
}
