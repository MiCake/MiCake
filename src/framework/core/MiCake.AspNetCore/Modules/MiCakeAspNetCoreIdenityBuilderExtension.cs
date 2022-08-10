using MiCake.Core;
using MiCake.Core.Util.Reflection;
using MiCake.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.AspNetCore.Identity
{
    public static class MiCakeAspNetCoreIdenityBuilderExtension
    {
        /// <summary>
        /// Register <see cref="IMiCakeUser"/> to MiCake application,so that MiCake can identify user info.
        /// </summary>
        /// <typeparam name="TMiCakeUser">User inherit from <see cref="IMiCakeUser"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="identityOptionsConfig"></param>
        public static IMiCakeBuilder UseIdentity<TMiCakeUser>(this IMiCakeBuilder builder, Action<MiCakeIdentityOptions>? identityOptionsConfig = null)
            where TMiCakeUser : IMiCakeUser
        {
            return UseIdentity(builder, typeof(TMiCakeUser), identityOptionsConfig);
        }

        /// <summary>
        /// Register <see cref="IMiCakeUser"/> to MiCake application,so that MiCake can identify user info.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeUserType">User inherit from <see cref="IMiCakeUser"/></param>
        /// <param name="identityOptionsConfig"></param>
        public static IMiCakeBuilder UseIdentity(this IMiCakeBuilder builder, Type miCakeUserType, Action<MiCakeIdentityOptions>? identityOptionsConfig = null)
        {
            //Add identity core.
            builder.AddIdentityCore(miCakeUserType);

            //register user services
            var userKeyType = TypeHelper.GetGenericArguments(miCakeUserType, typeof(IMiCakeUser<>));
            if (userKeyType == null || userKeyType[0] == null)
                throw new ArgumentException($"Can not get the primary key type of IMiCakeUser,Please check your config when {nameof(UseIdentity)}.");

            builder.ConfigureApplication((app, services) =>
            {
                //make sure has add ihttpcontextaccessor.
                services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                var aspnetCoreCurrentUser = typeof(AspNetCoreMiCakeUser<>).MakeGenericType(userKeyType);
                //add ICurrentMiCakeUser
                services.Replace(new ServiceDescriptor(typeof(ICurrentMiCakeUser), aspnetCoreCurrentUser, ServiceLifetime.Scoped));

                Action<MiCakeIdentityOptions> defaultOptions = (s) => { };
                services.Configure(identityOptionsConfig ?? defaultOptions);
            });

            return builder;
        }
    }
}
