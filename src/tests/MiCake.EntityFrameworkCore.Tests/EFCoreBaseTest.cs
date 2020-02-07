using MiCake.EntityFrameworkCore.Tests.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

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
