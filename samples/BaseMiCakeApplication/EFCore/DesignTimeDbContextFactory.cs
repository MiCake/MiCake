using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BaseMiCakeApplication.EFCore
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BaseAppDbContext>
    {
        public BaseAppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BaseAppDbContext>();

            // get from the configuration file or environment variables in real scenarios
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");
            Console.WriteLine(connectionString);
            builder.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());

            return new BaseAppDbContext(builder.Options);
        }

        // use [dotnet ef migrations add <MigrationName>] to create migration
        // use [dotnet ef database update] to update database
    }
}
