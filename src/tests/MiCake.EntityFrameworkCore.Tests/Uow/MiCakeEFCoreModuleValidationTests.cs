using MiCake.EntityFrameworkCore.Modules;
using MiCake.EntityFrameworkCore.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Uow
{
    /// <summary>
    /// Tests for the MiCakeEFCoreModule execution strategy validation:
    /// retrying execution strategies are rejected for ambient writable units of work.
    /// </summary>
    public class MiCakeEFCoreModuleValidationTests
    {
        private static ServiceProvider BuildProvider<TDbContext>(Action<DbContextOptionsBuilder>? configure)
            where TDbContext : DbContext
        {
            var services = new ServiceCollection();
            services.AddDbContext<TDbContext>(opt => configure?.Invoke(opt));
            services.AddLogging();
            services.AddSingleton<IDbContextTypeRegistry, DbContextTypeRegistry>();

            var provider = services.BuildServiceProvider();
            var registry = provider.GetRequiredService<IDbContextTypeRegistry>();
            registry.RegisterDbContextType(typeof(TDbContext));

            return provider;
        }

        [Fact]
        public void ValidateExecutionStrategies_WithRetryingStrategy_ShouldThrowContextSpecificDiagnostic()
        {
            // Arrange - inject a retrying execution strategy factory (InMemory otherwise has none)
            using var provider = BuildProvider<ValidationDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString())
                   .ReplaceService<IExecutionStrategyFactory, AlwaysRetryStrategyFactory>());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                MiCakeEFCoreModule.ValidateExecutionStrategies(provider, provider.GetRequiredService<IDbContextTypeRegistry>()));

            Assert.Contains(nameof(ValidationDbContext), exception.Message);
            Assert.Contains("retries on failure", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateExecutionStrategies_WithNonRetryingStrategy_ShouldPass()
        {
            // Arrange - InMemory has no retrying execution strategy
            using var provider = BuildProvider<ValidationDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            // Act
            var exception = Record.Exception(() =>
                MiCakeEFCoreModule.ValidateExecutionStrategies(provider, provider.GetRequiredService<IDbContextTypeRegistry>()));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateDbContextLifetimes_WithScopedRegistration_ShouldPass()
        {
            // Arrange
            using var provider = BuildProvider<ValidationDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            // Act
            var exception = Record.Exception(() =>
                MiCakeEFCoreModule.ValidateDbContextLifetimes(
                    provider,
                    provider.GetRequiredService<IDbContextTypeRegistry>()));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateDbContextLifetimes_WhenSingletonOverridesScopedRegistration_ShouldThrow()
        {
            // Arrange - this models an unsupported registration added after MiCake's descriptor check
            var services = new ServiceCollection();
            services.AddDbContext<ValidationDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddSingleton<ValidationDbContext>(_ => new ValidationDbContext(
                new DbContextOptionsBuilder<ValidationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options));
            services.AddSingleton<IDbContextTypeRegistry, DbContextTypeRegistry>();

            using var provider = services.BuildServiceProvider();
            var registry = provider.GetRequiredService<IDbContextTypeRegistry>();
            registry.RegisterDbContextType(typeof(ValidationDbContext));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                MiCakeEFCoreModule.ValidateDbContextLifetimes(provider, registry));
            Assert.Contains(nameof(ValidationDbContext), exception.Message);
            Assert.Contains("scoped or pooled", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidateDbContextLifetimes_WhenTransientOverridesScopedRegistration_ShouldThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<ValidationDbContext>(opt =>
                opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddTransient<ValidationDbContext>(_ => new ValidationDbContext(
                new DbContextOptionsBuilder<ValidationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options));
            services.AddSingleton<IDbContextTypeRegistry, DbContextTypeRegistry>();

            using var provider = services.BuildServiceProvider();
            var registry = provider.GetRequiredService<IDbContextTypeRegistry>();
            registry.RegisterDbContextType(typeof(ValidationDbContext));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                MiCakeEFCoreModule.ValidateDbContextLifetimes(provider, registry));
            Assert.Contains(nameof(ValidationDbContext), exception.Message);
            Assert.Contains("scoped or pooled", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        public class ValidationDbContext : DbContext
        {
            public ValidationDbContext(DbContextOptions<ValidationDbContext> options) : base(options)
            {
            }
        }

        private sealed class AlwaysRetryStrategyFactory : IExecutionStrategyFactory
        {
            public IExecutionStrategy Create() => new AlwaysRetryStrategy();
        }

        private sealed class AlwaysRetryStrategy : IExecutionStrategy
        {
            public bool RetriesOnFailure => true;

            public ExecutionStrategyDependencies Dependencies => null!;

            public TResult Execute<TState, TResult>(
                TState state,
                Func<DbContext, TState, TResult> operation,
                Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded = null)
                => throw new NotSupportedException("Test strategy must not execute.");

            public Task<TResult> ExecuteAsync<TState, TResult>(
                TState state,
                Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
                Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded = null,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException("Test strategy must not execute.");
        }
    }
}
