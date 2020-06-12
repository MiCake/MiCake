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
        public static IMiCakeBuilder UseIdentity<TMiCakeUser>(this IMiCakeBuilder builder, Action<MiCakeJwtOptions> jwtOptions = null)
            where TMiCakeUser : IMiCakeUser
        {
            return UseIdentity(builder, typeof(TMiCakeUser), jwtOptions);
        }

        public static IMiCakeBuilder UseIdentity(this IMiCakeBuilder builder, Type miCakeUserType, Action<MiCakeJwtOptions> jwtOptions = null)
        {
            //Add identity core.
            builder.AddIdentityCore(miCakeUserType, jwtOptions);

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
    }
}
