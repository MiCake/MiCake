using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BaseMiCakeApplication.EFCore
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BaseAppDbContext>
    {
        public BaseAppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BaseAppDbContext>();
            builder.UseNpgsql("Host=localhost;Port=54320;Database=micake_db;Username=postgres;Password=a12345");
            return new BaseAppDbContext(builder.Options);
        }
    }
}
