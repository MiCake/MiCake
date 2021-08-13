using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.EFCore;
using BaseMiCakeApplication.Handlers;
using BaseMiCakeApplication.MiCakeFeatures;
using MiCake;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using System.Text;

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
                options.UseNpgsql("Host=localhost;Port=54320;Database=micake_db;Username=postgres;Password=a12345");
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddMiCakeWithDefault<BaseMiCakeModule, BaseAppDbContext>(
                miCakeConfig: config =>
                {
                    config.Handlers.Add(new DemoExceptionHanlder());
                },
                miCakeAspNetConfig: options =>
                {
                    options.UseCustomModel();
                    options.DataWrapperOptions.IsDebug = true;
                })
                .UseIdentity<User>()
                .UseJwt(options =>
                {
                    options.Issuer = "MiCake";
                    options.Audience = "MiCake";
                    options.SecurityKey = Encoding.Default.GetBytes("ASDFGHJKL:QWERTYUIOP");
                })
                .Build();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters()
                        {
                            ValidateAudience = false,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.Default.GetBytes("ASDFGHJKL:QWERTYUIOP")),
                            ValidIssuer = "MiCake",
                            ValidAudience = "MiCake",
                        };
                    });

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
