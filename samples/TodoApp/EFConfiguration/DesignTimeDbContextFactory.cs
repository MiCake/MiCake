using MiCake;
using MiCake.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TodoApp.EFConfiguration
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TodoAppContext>
    {
        public TodoAppContext CreateDbContext(string[] args)
        {
            var services = new ServiceCollection() as IServiceCollection;
            services.AddLogging();
            services.AddMiCakeServices<ToDoAppModule, TodoAppContext>(PresetAuditConstants.PostgreSql_GetDateFunc).Build();

            var optionsBuilder = new DbContextOptionsBuilder<TodoAppContext>();
            var config = GetConfiguration();
            var dbStr = config.GetConnectionString("Postgres");
            optionsBuilder.UseNpgsql(dbStr);

            return new TodoAppContext(optionsBuilder.Options, services.BuildServiceProvider());
        }

        private static IConfiguration GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            return builder.Build();
        }
    }
}
