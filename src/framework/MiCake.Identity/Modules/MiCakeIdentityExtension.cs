using MiCake.Audit.Core;
using MiCake.Core;
using MiCake.Core.Util;
using MiCake.Core.Util.Reflection;
using MiCake.Identity.Audit;
using MiCake.Identity.Authentication.JwtToken;
using MiCake.Identity.Authentication.JwtToken.Abstractions;
using MiCake.Identity.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace MiCake.Identity
{
    public static class MiCakeIdentityExtension
    {
        /// <summary>
        /// Add your user information to the MiCake, which will be tracked automatically.
        /// </summary>
        /// <typeparam name="TMiCakeUser"><see cref="IMiCakeUser{TKey}"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        public static IMiCakeBuilder AddIdentityCore<TMiCakeUser>(this IMiCakeBuilder builder)
            where TMiCakeUser : IMiCakeUser
        {
            return AddIdentityCore(builder, typeof(TMiCakeUser));
        }

        /// <summary>
        /// Add your user information to the MiCake, which will be tracked automatically.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeUserType"><see cref="IMiCakeUser{TKey}"/></param>
        /// <returns></returns>
        public static IMiCakeBuilder AddIdentityCore(this IMiCakeBuilder builder, Type miCakeUserType)
        {
            CheckValue.NotNull(miCakeUserType, nameof(miCakeUserType));

            if (!typeof(IMiCakeUser).IsAssignableFrom(miCakeUserType))
                throw new ArgumentException($"Wrong user type,must use {nameof(IMiCakeUser)} to register your micake user.");

            builder.ConfigureApplication((app, services) =>
            {
                //Add identity module.
                app.ModuleManager.AddMiCakeModule(typeof(MiCakeIdentityModule));

                //Early registration IdentityAuditProvider.
                var userKeyType = TypeHelper.GetGenericArguments(miCakeUserType, typeof(IMiCakeUser<>));

                if (userKeyType == null || userKeyType[0] == null)
                    throw new ArgumentException($"Can not get the primary key type of IMiCakeUser,Please check your config when AddIdentity().");

                var auditProviderType = typeof(IdentityAuditProvider<>).MakeGenericType(userKeyType[0]);
                services.AddScoped(typeof(IAuditProvider), auditProviderType);
            });

            return builder;
        }

        /// <summary>
        /// Add Jwt support:can use <see cref="IJwtAuthManager"/> to create jwt token or and refresh-token.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IMiCakeBuilder AddJwt(this IMiCakeBuilder builder, Action<MiCakeJwtOptions> options)
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
