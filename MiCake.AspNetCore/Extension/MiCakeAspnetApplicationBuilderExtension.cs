using MiCake.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.AspNetCore.Extension
{
    public static class MiCakeAspnetApplicationBuilderExtension
    {
        public static IApplicationBuilder InitMiCake(this IApplicationBuilder app)
        {
            var micakeApp = app.ApplicationServices.GetService(typeof(IMiCakeApplication));
            ((IMiCakeApplication)micakeApp)?.Init();

            return app;
        }

    }
}
