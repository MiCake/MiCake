using MiCake.Core.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaseMiCakeApplication.EFCore
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BaseAppDbContext>
    {
        public BaseAppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BaseAppDbContext>();

            var connectionString = "Server=localhost;Database=MiCakeApp;User=root;Password=yourpassword;";
            builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                             mySqlOptions => mySqlOptions.EnableRetryOnFailure());
                             
            return new BaseAppDbContext(builder.Options, ServiceLocator.Instance.Locator);
        }

        // use [dotnet ef migrations add <MigrationName>] to create migration
        // use [dotnet ef database update] to update database
    }
}
