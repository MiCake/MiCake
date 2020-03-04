using MiCake.Identity.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MiCake.Identity.Extensions
{
    public static class MiCakeIdentityExtension
    {
        //public static IMiCakeBuilder RegisterCurrentUser<TUserType>(this IMiCakeBuilder builder)
        //    where TUserType : CurrentUser
        //{
        //    var currentOptions = new CurrentUserOptions() { CurrentUserType = typeof(TUserType) };
        //    builder.Services.Replace(
        //        new ServiceDescriptor(typeof(IOptions<CurrentUserOptions>), currentOptions));

        //    var currentUserType = typeof(ICurrentUser<>).MakeGenericType(currentOptions.UserKeyType);
        //    builder.Services.Replace(
        //        new ServiceDescriptor(currentUserType, typeof(TUserType), ServiceLifetime.Transient));

        //    builder.Services.AddTransient<ISetAuditPropertyAbility, TUserType>();

        //    return builder;
        //}
    }
}
