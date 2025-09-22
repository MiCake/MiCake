using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Domain;
using MiCake.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.EntityFrameworkCore.Performance
{
    /// <summary>
    /// Performance benchmark showing the improvement from filtering changed entities only
    /// Run with: dotnet run -c Release --project MiCake.EntityFrameworkCore.Performance
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob]
    public class PerformanceBenchmark
    {
        private BenchmarkDbContext _context;
        private List<BenchmarkEntity> _entities;
        
        [Params(100, 1000, 10000)]
        public int EntityCount { get; set; }
        
        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            
            var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            _context = new BenchmarkDbContext(options, serviceProvider);
            _context.Database.EnsureCreated();
            
            // Create many entities, but only change a few
            _entities = new List<BenchmarkEntity>();
            for (int i = 0; i < EntityCount; i++)
            {
                _entities.Add(new BenchmarkEntity 
                { 
                    Name = $"Entity{i}", 
                    IsDeleted = false 
                });
            }
            
            _context.Entities.AddRange(_entities);
            _context.SaveChanges();
            
            // Only modify a small percentage of entities (realistic scenario)
            var entitiesToModify = Math.Max(1, EntityCount / 20); // 5% of entities
            for (int i = 0; i < entitiesToModify; i++)
            {
                _entities[i].Name = $"Modified{i}";
            }
        }
        
        [Benchmark]
        public void SaveChanges_WithOptimizedInterceptor()
        {
            // This will only process the ~5% of entities that were actually changed
            _context.SaveChanges();
        }
        
        [GlobalCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }
    }
    
    public class BenchmarkDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        
        public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options, IServiceProvider serviceProvider) 
            : base(options)
        {
            _serviceProvider = serviceProvider;
        }
        
        public DbSet<BenchmarkEntity> Entities { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseMiCakeConventions();
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseMiCakeInterceptors(_serviceProvider);
        }
    }
    
    public class BenchmarkEntity : Entity, ISoftDeletion
    {
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
    
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<PerformanceBenchmark>();
            Console.WriteLine(summary);
        }
    }
}