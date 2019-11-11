using MiCake.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core
{
    public class MiCakeApplictionFactory
    {
        public static IMiCakeApplication Create<TStartupModule>(IServiceCollection services, Action<IMiCakeApplicationOption> optionAction = null)
        {
            return new MiCakeMianApplication(typeof(TStartupModule), services, optionAction);
        }
    }
}
