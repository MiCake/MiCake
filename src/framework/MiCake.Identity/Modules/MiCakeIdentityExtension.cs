﻿using MiCake.Audit.Core;
using MiCake.Core;
using MiCake.Core.Util;
using MiCake.Core.Util.Reflection;
using MiCake.Identity;
using MiCake.Identity.Audit;
using MiCake.Identity.Modules;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake
{
    public static class MiCakeIdentityExtension
    {
        /// <summary>
        /// Add your user information to the MiCake, which will be tracked automatically.
        /// </summary>
        /// <typeparam name="TMiCakeUser"><see cref="IMiCakeUser{TKey}"/></typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        public static IMiCakeBuilder AddIdentity<TMiCakeUser>(this IMiCakeBuilder builder)
            where TMiCakeUser : IMiCakeUser
        {
            return AddIdentity(builder, typeof(TMiCakeUser));
        }

        /// <summary>
        /// Add your user information to the MiCake, which will be tracked automatically.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="miCakeUserType"><see cref="IMiCakeUser{TKey}"/></param>
        /// <returns></returns>
        public static IMiCakeBuilder AddIdentity(this IMiCakeBuilder builder, Type miCakeUserType)
        {
            CheckValue.NotNull(miCakeUserType, nameof(miCakeUserType));

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
    }
}
