using MiCake.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core
{
    public class MiCakeApplicationOption : IMiCakeApplicationOption
    {

        public MiCakeApplicationOption(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; set; }
    }
}
