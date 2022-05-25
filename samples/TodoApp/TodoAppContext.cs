using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Aggregates.Identity;
using TodoApp.Domain.Aggregates.Todo;
using TodoApp.EFConfiguration;

namespace TodoApp
{
    public class TodoAppContext : MiCakeDbContext
    {
        public DbSet<TodoItem> TodoItem { get; set; }
        public DbSet<TodoUser> TodoUser { get; set; }
        public DbSet<ConcernedTodo> ConcernedTodo { get; set; }

#pragma warning disable CS8618
        public TodoAppContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        {
        }

#pragma warning disable CS8618
        protected TodoAppContext()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ConfigureCustomModel();
        }
    }
}
