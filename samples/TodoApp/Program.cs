using MiCake;
using MiCake.AspNetCore.Identity;
using MiCake.Dapper;
using MiCake.Identity.Authentication.JwtToken;
using MiCake.SqlReader;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using TodoApp;
using TodoApp.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration)
                 .ReadFrom.Services(services)
                 .Enrich.FromLogContext();
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
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
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            new List<string>()
        }
    });
});

// Add EFCore
builder.Services.AddDbContext<TodoAppContext>(options =>
                {
                    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
                    options.LogTo(Console.WriteLine);
                });

// Get jwt config
JwtConfigModel jwtConfig = builder.Configuration.GetSection("JwtConfig").Get<JwtConfigModel>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateAudience = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(jwtConfig.SecurityKey!)),
                        ValidIssuer = jwtConfig.Issuer,
                        ValidAudience = jwtConfig.Audience,
                    };
                });

// Add MiCake,and choose some features.
builder.Services.AddMiCakeServices<ToDoAppModule, TodoAppContext>()
                .UseIdentity<int>()
                .UseSqlReader(options => { options.UseXmlFileProvider(xmlOpt => { xmlOpt.FolderPath = "QueryReader//files"; }); })
                .UseDapper(builder.Configuration.GetConnectionString("Postgres"))
                .UseJwt(options =>
                {
                    options.AccessTokenLifetime = (uint)jwtConfig.AccessTokenLifetime;
                    options.Issuer = jwtConfig.Issuer!;
                    options.Audience = jwtConfig.Audience!;
                    options.SecurityKey = Encoding.Default.GetBytes(jwtConfig.SecurityKey!);
                    options.RefreshTokenMode = RefreshTokenUsageMode.Recreate;
                    options.DeleteWhenExchangeRefreshToken = true;
                })
                .Build();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.StartMiCake();

app.Run();
