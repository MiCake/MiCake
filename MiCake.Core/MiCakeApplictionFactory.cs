using MiCake.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core
{
    public class MiCakeApplictionFactory
    {
        public static IMiCakeApplication Create<TStartupModule>(IServiceCollection services)
        {
            return new DefaultMiCakeApplicationProvider(typeof(TStartupModule), services);
        }
    }
}
