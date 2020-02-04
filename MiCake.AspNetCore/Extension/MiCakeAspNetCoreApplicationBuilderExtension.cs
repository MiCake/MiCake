using MiCake.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;
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
