using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions
{
    /// <summary>
    /// configure micake application.when startup called services.AddMiCake().
    /// </summary>
    public interface IMiCakeApplicationOption
    {
        IServiceCollection Services { get; set; }
    }
}
