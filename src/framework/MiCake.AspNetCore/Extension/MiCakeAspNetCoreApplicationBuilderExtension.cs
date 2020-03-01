using MiCake.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake
{
    public static class MiCakeAspNetCoreApplicationBuilderExtension
    {
        public static IApplicationBuilder InitMiCake(this IApplicationBuilder app)
        {
            var provider = app.ApplicationServices;
            provider.GetRequiredService<IMiCakeApplicationProvider>()
                    ?.Initialize(provider);

            return app;
        }

    }
}
