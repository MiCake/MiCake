using BaseMiCakeApplication.EFCore;
using BaseMiCakeApplication.Handlers;
using BaseMiCakeApplication.MiCakeFeatures;
using MiCake;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;

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

            services.AddDbContext<BaseAppDbContext>(options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), c =>
                {
                    c.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                }); 
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMiCakeWithDefault<BaseMiCakeModule, BaseAppDbContext>(
                miCakeConfig: config =>
                {
                    config.Handlers.Add(new DemoExceptionHanlder());
                },
                miCakeAspNetConfig: options =>
                {
                    // options.UseCustomModel();
                    // options.DataWrapperOptions.ShowStackTraceWhenError = true;
                })
                .Build();

            // Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Calliope.Dream.Web", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Description = "Authorization format : Bearer {token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new List<string>()
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MiCake Application"));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.StartMiCake();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
