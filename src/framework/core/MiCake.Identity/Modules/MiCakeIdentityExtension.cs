using MiCake.Core;
using MiCake.Core.Util;
using MiCake.Identity.Modules;

namespace MiCake.Identity
{
    public static class MiCakeIdentityExtension
    {
        /// <summary>
        /// Add your user information to the MiCake, which will be tracked automatically.
        /// </summary>
        /// <typeparam name="TUserIdType">the user id type.</typeparam>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        public static IMiCakeBuilder AddIdentityCore<TUserIdType>(this IMiCakeBuilder builder)
        {
            return AddIdentityCore(builder, typeof(TUserIdType));
        }

        /// <summary>
        /// Add your user information to the MiCake, which will be tracked automatically.
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="userIdType"></param>
        /// <returns></returns>
        public static IMiCakeBuilder AddIdentityCore(this IMiCakeBuilder builder, Type userIdType)
        {
            CheckValue.NotNull(userIdType, nameof(userIdType));

            builder.ConfigureApplication((app, services) =>
            {
                //Add identity module.
                app.SlotModule<MiCakeIdentityModule>();

                app.AddStartupTransientData(MiCakeIdentityModule.CurrentIdentityUserKeyType, userIdType);
            });

            return builder;
        }
    }
}
