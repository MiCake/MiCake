using MiCake.EntityFrameworkCore.Tests.Seed;
using Microsoft.Extensions.DependencyInjection;
namespace MiCake.EntityFrameworkCore.Tests
{
    public abstract class EFCoreBaseTest
    {
        public IServiceCollection Services { get; set; }

        public EFCoreBaseTest()
        {
            Services = new ServiceCollection();
            //Add EFCore in memory db.
            Services.AddEntityFrameworkInMemoryDatabase()
                    .AddDbContext<TestDbContext>();
        }
    }
}
