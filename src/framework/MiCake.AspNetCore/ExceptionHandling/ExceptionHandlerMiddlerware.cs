using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.ExceptionHandling
{
    /// <summary>
    /// Although <see cref="ExceptionDataWrapper"/>[<see cref="IAsyncExceptionFilter"/>] has been provided for action execution process, 
    /// Error information in other middleware scope needs to be intercepted
    /// </summary>
    internal class ExceptionHandlerMiddlerware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlerMiddlerware(RequestDelegate next)
        {

        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Call the next delegate/middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex) when (ex is MiCakeException)
            {

            }
            catch
            {

            }
        }
    }
}
