using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using System;

namespace BaseMiCakeApplication.EFCore
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BaseAppDbContext>
    {
        public BaseAppDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<BaseAppDbContext>();
            builder.UseMySql("Server=localhost;Database=micakeexample;User=root;Password=a12345;", mySqlOptions => mySqlOptions
                    .ServerVersion(new ServerVersion(new Version(10, 5, 0), ServerType.MariaDb)));
            return new BaseAppDbContext(builder.Options);
        }
    }
}
