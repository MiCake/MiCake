using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Pomelo.EntityFrameworkCore.MySql.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UowMiCakeApplication.AggregateRoots;

namespace UowMiCakeApplication.EFCore
{
    public class UowAppDbContext : DbContext
    {
        public UowAppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Itinerary> Itinerarys { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseMySql("Server=localhost;Database=uowexample;User=root;Password=a12345;", mySqlOptions => mySqlOptions
            //        .ServerVersion(new ServerVersion(new Version(10, 5, 0), ServerType.MariaDb)));
        }
    }
}
