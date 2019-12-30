using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

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
