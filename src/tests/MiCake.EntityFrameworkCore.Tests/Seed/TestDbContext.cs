using Microsoft.EntityFrameworkCore;
using System;

namespace MiCake.EntityFrameworkCore.Tests.Seed
{
    public class TestDbContext : MiCakeDbContext
    {
        public TestDbContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

        protected TestDbContext()
        {
        }
    }
}
