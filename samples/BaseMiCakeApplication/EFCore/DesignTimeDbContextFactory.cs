using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaseMiCakeApplication.EFCore
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BaseAppDbContext>
    {
        public BaseAppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BaseAppDbContext>();

            var connectionString = "Server=localhost;Database=MiCakeApp;User Id=sa;Password=a12345;";
            builder.UseSqlServer(connectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());

            return new BaseAppDbContext(builder.Options);
        }

        // use [dotnet ef migrations add <MigrationName>] to create migration
        // use [dotnet ef database update] to update database
    }
}
