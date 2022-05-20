using MiCake;
using MiCake.Audit;
using MiCake.Dapper;
using Serilog;
using TodoApp;

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

// Add MiCake,and choose some features.
builder.Services.AddMiCakeServices<ToDoAppModule, TodoAppContext>(PresetAuditConstants.PostgreSql_GetDateFunc)
                .UseDapper(builder.Configuration.GetConnectionString("Postgres"))
                .Build();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.StartMiCake();

app.Run();
