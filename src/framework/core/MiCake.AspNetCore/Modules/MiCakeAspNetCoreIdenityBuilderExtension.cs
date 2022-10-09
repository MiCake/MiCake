using MiCake.Core;
using MiCake.Core.Util;
using MiCake.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MiCake.AspNetCore.Identity
{
    public static class MiCakeAspNetCoreIdenityBuilderExtension
    {
        /// <summary>
        /// Register identity info to MiCake,so that MiCake can track user info.
        /// </summary>
        /// <typeparam name="TUserIdType">User id type.</typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="identityOptionsConfig"></param>
        public static IMiCakeBuilder UseIdentity<TUserIdType>(this IMiCakeBuilder builder, Action<MiCakeIdentityOptions>? identityOptionsConfig = null)
        {
            return UseIdentity(builder, typeof(TUserIdType), identityOptionsConfig);
        }

        /// <summary>
        /// Register identity info to MiCake,so that MiCake can track user info.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="userIdType">User inherit from <see cref="IMiCakeUser"/></param>
        /// <param name="identityOptionsConfig"></param>
        public static IMiCakeBuilder UseIdentity(this IMiCakeBuilder builder, Type userIdType, Action<MiCakeIdentityOptions>? identityOptionsConfig = null)
        {
            CheckValue.NotNull(userIdType, nameof(userIdType));

            //Add identity core.
            builder.AddIdentityCore(userIdType);

            builder.ConfigureApplication((app, services) =>
            {
                //make sure has add ihttpcontextaccessor.
                services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                var aspnetCoreCurrentUser = typeof(AspNetCoreMiCakeUser<>).MakeGenericType(userIdType);
                //add ICurrentMiCakeUser
                services.Replace(new ServiceDescriptor(typeof(ICurrentMiCakeUser), aspnetCoreCurrentUser, ServiceLifetime.Scoped));

                Action<MiCakeIdentityOptions> defaultOptions = (s) => { };
                services.Configure(identityOptionsConfig ?? defaultOptions);
            });

            return builder;
        }
    }
}
