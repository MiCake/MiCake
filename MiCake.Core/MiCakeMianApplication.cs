using System;
using System.Collections.Generic;
using System.Text;
using MiCake.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core
{
    public class MiCakeMianApplication : MiCakeApplication
    {
        public MiCakeMianApplication(
            Type startUpType,
            IServiceCollection services,
            Action<IMiCakeApplicationOption> optionAction = null) : base(startUpType, services, optionAction)
        {
            services.AddSingleton<IMiCakeApplication>(this);
        }
    }
}
