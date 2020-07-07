using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Tests.Seed
{
    public class TestDbContext : MiCakeDbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        protected TestDbContext()
        {
        }
    }
}
