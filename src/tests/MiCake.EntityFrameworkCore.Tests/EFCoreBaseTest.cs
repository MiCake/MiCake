using MiCake.EntityFrameworkCore.Tests.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.EntityFrameworkCore.Tests
{
    public abstract class EFCoreBaseTest
    {
        public EFCoreBaseTest()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<TestDbContext>();
        }
    }
}
