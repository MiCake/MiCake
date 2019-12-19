using System;
using MiCake.AspNetCore.Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using UowMiCakeApplication.EFCore;
using UowMiCakeApplication.Repositories;

namespace UowMiCakeApplication
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
            services.AddControllers();
            services.AddTransient(typeof(ItineraryRepository));

            services.AddDbContext<UowAppDbContext>(options =>
            {
                options.UseMySql("Server=localhost;Database=uowexample;User=root;Password=a12345;", mySqlOptions => mySqlOptions
                    .ServerVersion(new ServerVersion(new Version(10, 5, 0), ServerType.MariaDb)));
            });
            services.AddMiCake<UowMiCakeModule>();
            
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

            app.InitMiCake();

            app.UseMiddleware<UnitOfWorkMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
