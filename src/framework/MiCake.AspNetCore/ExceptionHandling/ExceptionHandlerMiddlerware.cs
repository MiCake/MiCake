using MiCake.AspNetCore.DataWrapper.Internals;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MiCake.AspNetCore.ExceptionHandling
{
    /// <summary>
    /// Although <see cref="ExceptionDataWrapper"/>[<see cref="IAsyncExceptionFilter"/>] has been provided for action execution process, 
    /// Error information in other middleware scope needs to be intercepted
    /// </summary>
    internal class ExceptionHandlerMiddlerware
    {
    }
}
