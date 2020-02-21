using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.Tests.Seed
{
    public class TestDbContext : DbContext
    {
        public TestDbContext([NotNull] DbContextOptions options) : base(options)
        {
        }

        protected TestDbContext()
        {
        }
    }
}
