using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiCake.AspNetCore.Extension;
using MiCake.Serilog;
using MiCake.Autofac;
using MiCake.DDD.Domain;
using BaseMiCakeApplication.Domain.Aggregates;
using System;
using MiCake.EntityFrameworkCore.Repository;

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
            services.AddControllers().AddControllersAsServices();

            services.AddTransient(typeof(IReadOnlyRepository<,>), (s =>
            {

                return null;
            }));

            services.AddMiCake<BaseMiCakeModule>(builer =>
            {
                builer.UseSerilog();
                builer.UseAutofac();
            });
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.InitMiCake();
        }
    }
}
