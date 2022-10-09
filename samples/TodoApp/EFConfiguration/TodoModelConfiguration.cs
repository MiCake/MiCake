using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Aggregates.Identity;
using TodoApp.Domain.Aggregates.Todo;

namespace TodoApp.EFConfiguration
{
    public static class TodoModelConfiguration
    {
        public static void ConfigureCustomModel(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TodoItem>().ToTable("TodoItem");
            modelBuilder.Entity<TodoItem>().HasKey(x => x.Id);
            modelBuilder.Entity<TodoItem>().HasIndex(x => x.AuthorId);

            modelBuilder.Entity<TodoUser>().ToTable("TodoUser");
            modelBuilder.Entity<TodoUser>().OwnsOne(s => s.Name);

            modelBuilder.Entity<ConcernedTodo>().ToTable("ConcernedTodo");
            modelBuilder.Entity<ConcernedTodo>().HasIndex(s => s.TodoId);
            modelBuilder.Entity<ConcernedTodo>().HasIndex(s => s.UserId);
        }
    }
}
