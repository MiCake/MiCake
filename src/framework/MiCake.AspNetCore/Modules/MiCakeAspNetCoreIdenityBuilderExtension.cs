using MiCake.AspNetCore.Identity;
using MiCake.Core;
using MiCake.Core.Util.Reflection;
using MiCake.Identity;
using MiCake.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace MiCake.AspNetCore.Modules
{
    public static class MiCakeAspNetCoreIdenityBuilderExtension
    {
        /// <summary>
        /// Register <see cref="IMiCakeUser"/> to MiCake application, who will be automatically audited and authenticated by MiCake.
        /// </summary>
        /// <typeparam name="TMiCakeUser">User inherit from <see cref="IMiCakeUser"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        public static IMiCakeBuilder UseIdentity<TMiCakeUser>(this IMiCakeBuilder builder)
            where TMiCakeUser : IMiCakeUser
        {
            return UseIdentity(builder, typeof(TMiCakeUser));
        }

        /// <summary>
        /// Register <see cref="IMiCakeUser"/> to MiCake application, who will be automatically audited and authenticated by MiCake.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeUserType">User inherit from <see cref="IMiCakeUser"/></param>
        public static IMiCakeBuilder UseIdentity(this IMiCakeBuilder builder, Type miCakeUserType)
        {
            //Add identity core.
            builder.AddIdentityCore(miCakeUserType);

            //register user services
            var userKeyType = TypeHelper.GetGenericArguments(miCakeUserType, typeof(IMiCakeUser<>));
            if (userKeyType == null || userKeyType[0] == null)
                throw new ArgumentException($"Can not get the primary key type of IMiCakeUser,Please check your config when AddIdentity().");

            builder.ConfigureApplication((app, services) =>
            {
                //make sure has add ihttpcontextaccessor.
                services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                var currentMiCakeUserType = typeof(CurrentMiCakeUser<>).MakeGenericType(userKeyType);
                var aspnetCoreCurrentUser = typeof(AspNetCoreMiCakeUser<>).MakeGenericType(userKeyType);
                //add ICurrentMiCakeUser
                services.Replace(new ServiceDescriptor(typeof(ICurrentMiCakeUser), aspnetCoreCurrentUser, ServiceLifetime.Scoped));
                services.Replace(new ServiceDescriptor(currentMiCakeUserType, aspnetCoreCurrentUser, ServiceLifetime.Scoped));
            });

            return builder;
        }

        /// <summary>
        /// Register <see cref="IMiCakeUser"/> to MiCake application, who will be automatically audited and authenticated by MiCake.
        /// </summary>
        /// <typeparam name="TMiCakeUser">User inherit from <see cref="IMiCakeUser"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="jwtSupportOptionsConfig">config <see cref="MiCakeJwtOptions"/>.This optios will be used by <see cref="IJwtSupporter"/></param>
        public static IMiCakeBuilder UseIdentity<TMiCakeUser>(this IMiCakeBuilder builder, Action<MiCakeJwtOptions> jwtSupportOptionsConfig)
            where TMiCakeUser : IMiCakeUser
        {
            return UseIdentity(builder, typeof(TMiCakeUser), jwtSupportOptionsConfig);
        }

        /// <summary>
        /// Register <see cref="IMiCakeUser"/> to MiCake application, who will be automatically audited and authenticated by MiCake.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeUserType">User inherit from <see cref="IMiCakeUser"/></param>
        /// <param name="jwtSupportOptionsConfig">config <see cref="MiCakeJwtOptions"/>.This optios will be used by <see cref="IJwtSupporter"/></param>
        public static IMiCakeBuilder UseIdentity(this IMiCakeBuilder builder, Type miCakeUserType, Action<MiCakeJwtOptions> jwtSupportOptionsConfig)
        {
            UseIdentity(builder, miCakeUserType);

            if (jwtSupportOptionsConfig != null)
            {
                builder.ConfigureApplication((app, services) =>
                {
                    services.PostConfigure(jwtSupportOptionsConfig);
                });
            }
            return builder;
        }
    }
}
