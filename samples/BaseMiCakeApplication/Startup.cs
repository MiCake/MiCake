using BaseMiCakeApplication.EFCore;
using MiCake;
using MiCake.AspNetCore;
using MiCake.Audit;
using MiCake.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using System;

namespace BaseMiCakeApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options =>
            {
            });

            services.AddHealthChecks();

            services.AddDbContext<BaseAppDbContext>(options =>
            {
                options.UseMySql("Server=localhost;Database=micakeexample;User=root;Password=a12345;", mySqlOptions => mySqlOptions
                    .ServerVersion(new ServerVersion(new Version(10, 5, 0), ServerType.MariaDb)));
            });

            services.AddMiCake<BaseMiCakeModule>()
                    .UseAspNetCore()
                    .UseAudit()
                    .UseEFCore<BaseAppDbContext>()
                    .Build();

            //Add Swagger
            services.AddSwaggerDocument(document => document.DocumentName = "MiCake Demo Application");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.StartMiCake();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseOpenApi();
            app.UseSwaggerUi3();
        }
    }
}
