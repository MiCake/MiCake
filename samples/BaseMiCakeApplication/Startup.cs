using BaseMiCakeApplication.EFCore;
using BaseMiCakeApplication.MiCakeFeatures;
using FluentValidation;
using FluentValidation.AspNetCore;
using MiCake.AspNetCore.ApiLogging;
using MiCake.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BaseMiCakeApplication
{
    /// <summary>
    /// Application startup configuration.
    /// </summary>
    /// <remarks>
    /// This class demonstrates:
    /// 1. MiCake framework registration
    /// 2. Entity Framework Core configuration
    /// 3. Fluent Validation setup
    /// 4. Swagger/OpenAPI documentation
    /// 5. Proper ASP.NET Core middleware configuration
    /// </remarks>
    public class Startup
    {
        /// <summary>
        /// Gets the application configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Initializes a new instance of the Startup class.
        /// </summary>
        /// <param name="configuration">The application configuration</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Configures application services during startup.
        /// </summary>
        /// <remarks>
        /// This method is called by the runtime to add services to the DI container.
        /// Order matters for dependencies - configure base services first.
        /// </remarks>
        public void ConfigureServices(IServiceCollection services)
        {
            // Add ASP.NET Core services
            services.AddControllers(options =>
            {
                // Controller options can be configured here
            });

            // Configure Entity Framework Core
            services.AddDbContext<BaseAppDbContext>(options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseNpgsql(connectionString);
            });

            // Register Fluent Validation
            services.AddValidatorsFromAssemblyContaining<Startup>();
            services.AddFluentValidationAutoValidation();

            // Register HTTP context accessor
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Register custom API log writer
            // This demonstrates how to implement a custom IApiLogWriter
            // You can replace this with your own implementation (e.g., database, Elasticsearch, file)
            services.AddSingleton<IApiLogWriter, ConsoleApiLogWriter>();

            // Register and configure MiCake framework
            services.AddMiCakeWithDefault<BaseMiCakeModule, BaseAppDbContext>(options =>
            {
                // Configure MiCake application options
                options.AppConfig = app =>
                {
                    // Application configuration
                };

                // Configure MiCake ASP.NET Core options (including API Logging)
                options.AspNetConfig = asp =>
                {
                    asp.UseApiLogging = true;
                    asp.ApiLoggingOptions.SensitiveFields = ["password", "token"];
                };
            })
            .Build();

            // Configure Swagger/OpenAPI
            services.AddSwaggerGen();
        }

        /// <summary>
        /// Configures the HTTP request pipeline.
        /// </summary>
        /// <remarks>
        /// This method is called by the runtime to configure how the application handles requests.
        /// The order of middleware registration is important.
        /// </remarks>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Configure development-specific middleware
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable Swagger documentation
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MiCake Sample Application v1"));

            // Redirect HTTP to HTTPS
            app.UseHttpsRedirection();

            // Enable routing
            app.UseRouting();

            // Authentication and Authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            // Initialize MiCake framework (MUST be called before UseEndpoints)
            app.StartMiCake();

            // Configure endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", () => "MiCake Framework Sample Application is running");
            });
        }
    }
}
