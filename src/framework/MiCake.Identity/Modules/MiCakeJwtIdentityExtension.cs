using MiCake.Core;
using MiCake.Identity;
using MiCake.Identity.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;

namespace MiCake
{
    public static class MiCakeJwtIdentityExtension
    {
        /// <summary>
        /// Add MiCake identity and default Jwt Bearer authentication services.
        /// 
        /// <para>
        ///     If <paramref name="addAspNetJwtBearerService"/> is true,will add Microsoft.AspNetCore.Authentication.JwtBearer automatic.
        ///     You no longer need to call the addauthentication extension method.
        ///     If you have special validation configuration requirements, you need to set this parameter to false
        /// </para>
        /// </summary>
        /// <typeparam name="TMiCakeUser"><see cref="IMiCakeUser{TKey}"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="micakeJwtOptions"><see cref="MiCakeJwtOptions"/> is use to set jwt configuration item.</param>
        /// <param name="addAspNetJwtBearerService">Need to add Microsoft.AspNetCore.Authentication.JwtBearer services.Default is true.</param>
        /// <returns></returns>
        public static IMiCakeBuilder AddIdentityWithJwt<TMiCakeUser>(
            this IMiCakeBuilder builder,
            Action<MiCakeJwtOptions> micakeJwtOptions,
            bool addAspNetJwtBearerService = true)
            where TMiCakeUser : IMiCakeUser
        {
            return AddIdentityWithJwt(builder, typeof(TMiCakeUser), micakeJwtOptions, addAspNetJwtBearerService);
        }

        /// <summary>
        /// Add MiCake identity and default Jwt Bearer authentication services.
        /// 
        /// <para>
        ///     If <paramref name="addAspNetJwtBearerService"/> is true,will add Microsoft.AspNetCore.Authentication.JwtBearer automatic.
        ///     You no longer need to call the addauthentication extension method.
        ///     If you have special validation configuration requirements, you need to set this parameter to false
        /// </para>
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeUserType">The type of <see cref="IMiCakeUser{TKey}"/></param>
        /// <param name="optionsAction"><see cref="MiCakeJwtOptions"/> is use to set jwt configuration item.</param>
        /// <param name="addAspNetJwtBearerService">Need to add Microsoft.AspNetCore.Authentication.JwtBearer services.Default is true.</param>
        /// <returns></returns>
        public static IMiCakeBuilder AddIdentityWithJwt(
            this IMiCakeBuilder builder,
            Type miCakeUserType,
            Action<MiCakeJwtOptions> optionsAction = null,
            bool addAspNetJwtBearerService = true)
        {
            //Add Core module and services.
            builder.AddIdentity(miCakeUserType);

            var micakeJwtOptions = new MiCakeJwtOptions();
            optionsAction?.Invoke(micakeJwtOptions);

            builder.ConfigureApplication((app, services) =>
            {
                services.Configure<MiCakeJwtOptions>(op =>
                {
                    op.FromOtherOptions(micakeJwtOptions);
                });

                if (addAspNetJwtBearerService)
                {
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(options =>
                            {
                                options.TokenValidationParameters = new TokenValidationParameters()
                                {
                                    IssuerSigningKey = new SymmetricSecurityKey(micakeJwtOptions.SecurityKey),
                                    ValidIssuer = micakeJwtOptions.Issuer,
                                    ValidAudience = micakeJwtOptions.Audience,
                                };
                            });
                }
            });

            return builder;
        }

    }
}
