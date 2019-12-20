using MiCake.Core.Abstractions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UowMiCakeApplication.Exception
{
    public class MiCakeExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var exceptionInfo = context.Exception;
        }
    }
}
