using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MiCake.IntegrationTests.Infrastructure
{
    public class TestDbContext : MiCakeDbContext
    {
        private readonly ILogger<TestDbContext>? _logger;

        public DbSet<Fakes.Product> Products { get; set; }
        public DbSet<Fakes.Order> Orders { get; set; }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public TestDbContext(DbContextOptions<TestDbContext> options, ILogger<TestDbContext> logger) : base(options)
        {
            _logger = logger;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Fakes.Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Fakes.Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _logger?.LogInformation("TestDbContext.OnConfiguring called");
            base.OnConfiguring(optionsBuilder); // This should call UseMiCakeInterceptors()
            _logger?.LogInformation("TestDbContext.OnConfiguring completed");
        }
    }
}
