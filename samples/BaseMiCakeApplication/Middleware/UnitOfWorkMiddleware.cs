using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Middleware
{
    public class UnitOfWorkMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;

        public UnitOfWorkMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
        {
            _next = next;
            _serviceProvider = serviceProvider;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            using (var s = _serviceProvider.CreateScope())
            {
                await _next(httpContext);
            }
        }
    }
}
