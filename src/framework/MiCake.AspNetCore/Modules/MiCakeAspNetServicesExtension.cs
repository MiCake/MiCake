using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.Core
{
    /// <summary>
    /// Extension methods for integrating MiCake with ASP.NET Core
    /// </summary>
    public static class MiCakeAspNetServicesExtension
    {
        /// <summary>
        /// Start MiCake application.
        /// This method initializes and starts all registered MiCake modules.
        /// </summary>
        /// <param name="applicationBuilder"><see cref="IApplicationBuilder"/></param>
        public static void StartMiCake(this IApplicationBuilder applicationBuilder)
        {
            var micakeApp = applicationBuilder.ApplicationServices.GetService<IMiCakeApplication>() ??
                                    throw new InvalidOperationException(
                                        $"Cannot find the instance of {nameof(IMiCakeApplication)}. " +
                                        $"Please ensure you have called AddMiCake() in ConfigureServices method.");

            // Start the application
            micakeApp.Start();
        }

        /// <summary>
        /// Shut down MiCake application.
        /// This method gracefully shuts down all registered MiCake modules.
        /// </summary>
        /// <param name="applicationBuilder"><see cref="IApplicationBuilder"/></param>
        public static void ShutdownMiCake(this IApplicationBuilder applicationBuilder)
        {
            var micakeApp = applicationBuilder.ApplicationServices.GetService<IMiCakeApplication>() ??
                                    throw new InvalidOperationException(
                                        $"Cannot find the instance of {nameof(IMiCakeApplication)}. " +
                                        $"Please ensure you have called AddMiCake() in ConfigureServices method.");

            micakeApp.ShutDown();
        }
    }
}
