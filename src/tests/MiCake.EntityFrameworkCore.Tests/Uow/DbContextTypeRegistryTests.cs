using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Unit tests for DbContextTypeRegistry.
    /// Tests registration and retrieval of DbContext types.
    /// </summary>
    public class DbContextTypeRegistryTests
    {
        #region RegisterDbContextType Tests

        [Fact]
        public void RegisterDbContextType_WithValidDbContextType_ShouldRegisterSuccessfully()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();

            // Act
            registry.RegisterDbContextType(typeof(TestRegistryDbContext));

            // Assert
            var types = registry.GetRegisteredTypes();
            Assert.Single(types);
            Assert.Contains(typeof(TestRegistryDbContext), types);
        }

        [Fact]
        public void RegisterDbContextType_WithNullType_ShouldThrowArgumentNullException()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => registry.RegisterDbContextType(null!));
        }

        [Fact]
        public void RegisterDbContextType_WithNonDbContextType_ShouldThrowArgumentException()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                registry.RegisterDbContextType(typeof(string)));
            Assert.Contains("must inherit from DbContext", exception.Message);
        }

        [Fact]
        public void RegisterDbContextType_WithSameTypeTwice_ShouldNotDuplicate()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();

            // Act
            registry.RegisterDbContextType(typeof(TestRegistryDbContext));
            registry.RegisterDbContextType(typeof(TestRegistryDbContext));

            // Assert
            var types = registry.GetRegisteredTypes();
            Assert.Single(types);
        }

        [Fact]
        public void RegisterDbContextType_WithMultipleTypes_ShouldRegisterAll()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();

            // Act
            registry.RegisterDbContextType(typeof(TestRegistryDbContext));
            registry.RegisterDbContextType(typeof(AnotherRegistryDbContext));

            // Assert
            var types = registry.GetRegisteredTypes();
            Assert.Equal(2, types.Count);
            Assert.Contains(typeof(TestRegistryDbContext), types);
            Assert.Contains(typeof(AnotherRegistryDbContext), types);
        }

        [Fact]
        public void RegisterDbContextType_WithDerivedDbContextType_ShouldRegisterSuccessfully()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();

            // Act
            registry.RegisterDbContextType(typeof(DerivedDbContext));

            // Assert
            var types = registry.GetRegisteredTypes();
            Assert.Single(types);
            Assert.Contains(typeof(DerivedDbContext), types);
        }

        #endregion

        #region GetRegisteredTypes Tests

        [Fact]
        public void GetRegisteredTypes_WhenEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();

            // Act
            var types = registry.GetRegisteredTypes();

            // Assert
            Assert.NotNull(types);
            Assert.Empty(types);
        }

        [Fact]
        public void GetRegisteredTypes_ShouldReturnReadOnlyList()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();
            registry.RegisterDbContextType(typeof(TestRegistryDbContext));

            // Act
            var types = registry.GetRegisteredTypes();

            // Assert
            Assert.IsAssignableFrom<IReadOnlyList<Type>>(types);
        }

        [Fact]
        public void GetRegisteredTypes_ShouldReturnDefensiveCopy()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();
            registry.RegisterDbContextType(typeof(TestRegistryDbContext));

            // Act
            var types1 = registry.GetRegisteredTypes();
            registry.RegisterDbContextType(typeof(AnotherRegistryDbContext));
            var types2 = registry.GetRegisteredTypes();

            // Assert - First list should not be affected by later registrations
            Assert.Single(types1);
            Assert.Equal(2, types2.Count);
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task RegisterDbContextType_ShouldBeThreadSafe()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();
            var exceptions = new List<Exception>();

            // Act - Register from multiple threads
            var tasks = new System.Threading.Tasks.Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        registry.RegisterDbContextType(typeof(TestRegistryDbContext));
                        registry.RegisterDbContextType(typeof(AnotherRegistryDbContext));
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert
            Assert.Empty(exceptions);
            var types = registry.GetRegisteredTypes();
            Assert.Equal(2, types.Count);
        }

        [Fact]
        public async Task GetRegisteredTypes_ShouldBeThreadSafe()
        {
            // Arrange
            var registry = new DbContextTypeRegistry();
            registry.RegisterDbContextType(typeof(TestRegistryDbContext));
            var exceptions = new List<Exception>();

            // Act - Read from multiple threads while registering
            var tasks = new System.Threading.Tasks.Task[10];
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        if (index % 2 == 0)
                        {
                            var _ = registry.GetRegisteredTypes();
                        }
                        else
                        {
                            registry.RegisterDbContextType(typeof(AnotherRegistryDbContext));
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert
            Assert.Empty(exceptions);
        }

        #endregion

        #region Helper Classes

        public class TestRegistryDbContext : DbContext
        {
            public TestRegistryDbContext() : base() { }
            public TestRegistryDbContext(DbContextOptions options) : base(options) { }
        }

        public class AnotherRegistryDbContext : DbContext
        {
            public AnotherRegistryDbContext() : base() { }
            public AnotherRegistryDbContext(DbContextOptions options) : base(options) { }
        }

        public class DerivedDbContext : TestRegistryDbContext
        {
            public DerivedDbContext() : base() { }
            public DerivedDbContext(DbContextOptions options) : base(options) { }
        }

        #endregion
    }
}
