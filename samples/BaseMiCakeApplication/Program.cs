using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BaseMiCakeApplication
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates and configures the host builder.
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <returns>A configured IHostBuilder</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
