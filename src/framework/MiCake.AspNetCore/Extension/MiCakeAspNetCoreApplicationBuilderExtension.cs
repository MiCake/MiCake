using MiCake.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.AspNetCore.Extension
{
    public static class MiCakeAspNetCoreApplicationBuilderExtension
    {
        public static IApplicationBuilder InitMiCake(this IApplicationBuilder app)
        {
            var provider = app.ApplicationServices;
            var micakeApp = provider.GetRequiredService(typeof(IMiCakeApplicationProvider));
            ((IMiCakeApplicationProvider)micakeApp)?.Initialize(provider);

            return app;
        }

    }
}
