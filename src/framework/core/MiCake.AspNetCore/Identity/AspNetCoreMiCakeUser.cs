using MiCake.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Security.Claims;

namespace MiCake.AspNetCore.Identity
{
    internal class AspNetCoreMiCakeUser<TKey> : CurrentMiCakeUser<TKey>, IAspNetCoreMiCakeUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MiCakeIdentityOptions _options;

        public ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public AspNetCoreMiCakeUser(IHttpContextAccessor httpContextAccessor, IOptions<MiCakeIdentityOptions> options) : base()
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
        }

        public override TKey? GetUserID()
        {
            var userIDClaim = _httpContextAccessor.HttpContext?.User?.Claims
                                                  .FirstOrDefault(s => s.Type.Equals(_options.UserIdClaimName));

            if (userIDClaim == null)
                return default;

            //convert string to TKey type.
            var userId = (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(userIDClaim.Value)!;

            return userId;
        }
    }
}
