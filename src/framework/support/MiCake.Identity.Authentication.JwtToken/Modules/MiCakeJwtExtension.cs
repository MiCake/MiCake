using MiCake.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.Identity.Authentication.JwtToken
{
    public static class MiCakeJwtExtension
    {
        /// <summary>
        /// Add Jwt support:can use <see cref="IJwtAuthManager"/> to create jwt token or and refresh-token.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IMiCakeBuilder UseJwt(this IMiCakeBuilder builder, Action<MiCakeJwtOptions> options)
        {
            builder.ConfigureApplication((app, services) =>
            {
                services.TryAddScoped<IJwtAuthManager, JwtAuthManager>();
                services.TryAddScoped<IRefreshTokenService, DefaultRefreshTokenService>();

                services.TryAddScoped<IRefreshTokenStore, DefaultRefreshTokenStore>();
                services.TryAddScoped<IRefreshTokenHandleGenerator, DefaultRefreshTokenHandleGenerator>();
                services.Configure(options);
            });

            return builder;
        }
    }
}
