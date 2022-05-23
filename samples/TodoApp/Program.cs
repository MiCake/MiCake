using MiCake;
using MiCake.Audit;
using MiCake.Dapper;
using MiCake.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
                 .Enrich.FromLogContext()
                 .WriteTo.Console();
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add EFCore
builder.Services.AddDbContext<TodoAppContext>(options =>
                {
                    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
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
builder.Services.AddMiCakeServices<ToDoAppModule, TodoAppContext>(PresetAuditConstants.PostgreSql_GetDateFunc)
                .UseDapper(builder.Configuration.GetConnectionString("Postgres"))
                .UseJwt(options =>
                {
                    options.AccessTokenLifetime = (uint)jwtConfig.AccessTokenLifetime;
                    options.Issuer = jwtConfig.Issuer!;
                    options.Audience = jwtConfig.Audience!;
                    options.SecurityKey = Encoding.Default.GetBytes(jwtConfig.SecurityKey!);
                    options.RefreshTokenMode = MiCake.Identity.Authentication.JwtToken.RefreshTokenUsageMode.Reuse;
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
