using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UowMiCakeApplication.Exception;

namespace UowMiCakeApplication
{
    public class ConfigExceptionFilterOption : IPostConfigureOptions<MvcOptions>
    {
        public void PostConfigure(string name, MvcOptions options)
        {
            options.Filters.Add<MiCakeExceptionFilter>();
        }
    }
}
