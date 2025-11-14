using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace BaseMiCakeApplication.EFCore
{
    /// <summary>
    /// The main database context for the application.
    /// </summary>
    /// <remarks>
    /// This context demonstrates:
    /// 1. Inheritance from MiCakeDbContext for DDD support
    /// 2. Proper DbSet configuration for aggregate roots
    /// 3. Model configuration for value objects (owned types)
    /// </remarks>
    public class BaseAppDbContext : MiCakeDbContext
    {
        /// <summary>
        /// Gets or sets the collection of books.
        /// </summary>
        public virtual DbSet<Book> Books { get; set; }

        /// <summary>
        /// Gets or sets the collection of users.
        /// </summary>
        public virtual DbSet<User> Users { get; set; }

        /// <summary>
        /// Initializes a new instance of the BaseAppDbContext.
        /// </summary>
        /// <param name="options">The DbContext options</param>
        public BaseAppDbContext(DbContextOptions options) : base(options)
        {
        }

        /// <summary>
        /// Configures the database options.
        /// </summary>
        /// <remarks>
        /// The logging is enabled for development/debugging purposes.
        /// Comment out in production for better performance.
        /// </remarks>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            #if DEBUG
            // Enable SQL logging in debug builds for development
            optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            #endif
        }

        /// <summary>
        /// Configures the entity models and relationships.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: Always call base.OnModelCreating first to ensure MiCake's
        /// DDD entity configuration is applied before custom configuration.
        /// </remarks>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Book entity
            modelBuilder.Entity<Book>(entity =>
            {
                // Configure the BookAuthor value object as an owned entity
                entity.OwnsOne(s => s.Author, author =>
                {
                    // Column naming for the owned type
                    author.Property(a => a.FirstName)
                        .HasColumnName("AuthorFirstName")
                        .HasMaxLength(100)
                        .IsRequired();

                    author.Property(a => a.LastName)
                        .HasColumnName("AuthorLastName")
                        .HasMaxLength(100)
                        .IsRequired();
                });

                // Configure other properties
                entity.Property(b => b.BookName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(b => b.Description)
                    .HasMaxLength(1000);

                entity.Property(b => b.ISBN)
                    .HasMaxLength(20);

                // Add an index on BookName for faster queries
                entity.HasIndex(b => b.BookName);
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(u => u.Phone)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(u => u.Email)
                    .HasMaxLength(256);

                entity.Property(u => u.Avatar)
                    .HasMaxLength(500);

                // Add an index on Phone for faster lookups
                entity.HasIndex(u => u.Phone)
                    .IsUnique();

                // Configure soft deletion
                entity.HasQueryFilter(u => !u.IsDeleted);
            });

            // Apply MiCake's DDD entity configuration
            base.OnModelCreating(modelBuilder);
        }
    }
}
