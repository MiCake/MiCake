
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Tests.Seed
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        protected TestDbContext()
        {
        }
    }
}
