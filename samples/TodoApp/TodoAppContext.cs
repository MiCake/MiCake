using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TodoApp
{
    public class TodoAppContext : MiCakeDbContext
    {
        public TodoAppContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

        protected TodoAppContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
