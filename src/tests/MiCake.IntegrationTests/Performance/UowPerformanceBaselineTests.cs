using MiCake.DDD.Infrastructure;
using MiCake.DDD.Infrastructure.Lifetime;
using MiCake.DDD.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace MiCake.IntegrationTests.Performance
{
    /// <summary>
    /// Serializes baseline measurement so process-level allocation counters stay clean.
    /// </summary>
    [CollectionDefinition("PerformanceBaseline", DisableParallelization = true)]
    public sealed class PerformanceBaselineCollection
    {
    }

    /// <summary>
    /// Repeatable performance baseline for the common read and write paths required by the
    /// transaction-reliability acceptance: no-UoW reads, tracked writes with commit,
    /// generated-identity flush, lifecycle handlers, and immediate transaction activation.
    /// Each scenario runs warmup iterations followed by measured iterations and reports
    /// the elapsed distribution (min/median/p90) and allocated bytes for the measured set.
    /// Run with: dotnet test --filter "Category=Performance" --project src/tests/MiCake.IntegrationTests
    /// </summary>
    [Trait("Category", "Performance")]
    [Collection("PerformanceBaseline")]
    public class UowPerformanceBaselineTests
    {
        private const int WarmupIterations = 5;
        private const int MeasureIterations = 200;

        private readonly ITestOutputHelper _output;

        public UowPerformanceBaselineTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Read_NoUoW_RecordsElapsedAndAllocations()
        {
            using var fixture = new Uow.SqliteUnitOfWorkFixture();
            using var provider = fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            var result = await MeasureAsync(provider, "ReadNoUoW", async sp =>
            {
                await using var scope = sp.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<Uow.UowAcceptanceDbContext>();
                _ = await context.Aggregates.CountAsync();
            });

            Assert.Equal(MeasureIterations, result.Iterations);
        }

        [Fact]
        public async Task TrackedWrite_Commit_RecordsElapsedAndAllocations()
        {
            using var fixture = new Uow.SqliteUnitOfWorkFixture();
            using var provider = fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            var counter = 0;
            var result = await MeasureAsync(provider, "TrackedWriteCommit", async sp =>
            {
                await using var scope = sp.CreateAsyncScope();
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = Uow.SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new Uow.UowAcceptanceAggregate($"write-{Interlocked.Increment(ref counter)}"));
                await uow.CommitAsync();
            });

            Assert.Equal(MeasureIterations + WarmupIterations, await CountAsync(provider));
        }

        [Fact]
        public async Task Flush_GeneratedIdentity_RecordsElapsedAndAllocations()
        {
            using var fixture = new Uow.SqliteUnitOfWorkFixture();
            using var provider = fixture.BuildProvider();
            await EnsureCreatedAsync(provider);

            var counter = 0;
            var result = await MeasureAsync(provider, "FlushIdentity", async sp =>
            {
                await using var scope = sp.CreateAsyncScope();
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = Uow.SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                var aggregate = new Uow.UowAcceptanceAggregate($"flush-{Interlocked.Increment(ref counter)}");
                context.Aggregates.Add(aggregate);
                Assert.Equal(0, aggregate.Id);
                await uow.FlushAsync();
                Assert.NotEqual(0, aggregate.Id);
                await uow.CommitAsync();
            });

            Assert.Equal(MeasureIterations + WarmupIterations, await CountAsync(provider));
        }

        [Fact]
        public async Task LifecycleHandlers_RecordsElapsedAndAllocations()
        {
            using var fixture = new Uow.SqliteUnitOfWorkFixture();
            using var provider = fixture.BuildProvider(configure: s =>
            {
                s.AddScoped<IRepositoryPreSaveChanges, NoOpPreSaveHandler>();
                s.AddScoped<IRepositoryPostSaveChanges, NoOpPostSaveHandler>();
            });
            await EnsureCreatedAsync(provider);

            var counter = 0;
            var result = await MeasureAsync(provider, "LifecycleHandlers", async sp =>
            {
                await using var scope = sp.CreateAsyncScope();
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = Uow.SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new Uow.UowAcceptanceAggregate($"lifecycle-{Interlocked.Increment(ref counter)}"));
                await uow.CommitAsync();
            });

            Assert.Equal(MeasureIterations + WarmupIterations, await CountAsync(provider));
        }

        [Fact]
        public async Task ImmediateActivation_TwoContexts_RecordsElapsedAndAllocations()
        {
            using var fixture = new Uow.SqliteUnitOfWorkFixture();
            using var provider = fixture.BuildProvider(registerSecondaryContext: true);
            await EnsureCreatedAsync(provider);

            var result = await MeasureAsync(provider, "ImmediateActivation", async sp =>
            {
                await using var scope = sp.CreateAsyncScope();
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync(UnitOfWorkOptions.Immediate);
                Assert.True(uow.HasActiveTransactions);
            });

            Assert.Equal(MeasureIterations, result.Iterations);
        }

        private async Task<ScenarioResult> MeasureAsync(
            IServiceProvider provider,
            string name,
            Func<IServiceProvider, Task> operation)
        {
            for (var i = 0; i < WarmupIterations; i++)
            {
                await operation(provider);
            }

            var samples = new List<double>(MeasureIterations);
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            for (var i = 0; i < MeasureIterations; i++)
            {
                var sw = Stopwatch.StartNew();
                await operation(provider);
                sw.Stop();
                samples.Add(sw.Elapsed.TotalMilliseconds);
            }

            var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

            samples.Sort();
            var result = new ScenarioResult(
                name,
                MedianMs: samples[MeasureIterations / 2],
                P90Ms: samples[(int)(MeasureIterations * 0.9)],
                MinMs: samples[0],
                AllocatedBytes: allocated,
                Iterations: MeasureIterations);

            _output.WriteLine(result.ToLine());
            return result;
        }

        private static async Task EnsureCreatedAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<Uow.UowAcceptanceDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        private static async Task<int> CountAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<Uow.UowAcceptanceDbContext>();
            return await context.Aggregates.CountAsync();
        }

        private sealed record ScenarioResult(
            string Name,
            double MedianMs,
            double P90Ms,
            double MinMs,
            long AllocatedBytes,
            long Iterations)
        {
            public string ToLine()
                => $"{Name}: median={MedianMs:F2}ms p90={P90Ms:F2}ms min={MinMs:F2}ms allocated={AllocatedBytes} bytes ({Iterations} iterations)";
        }

        private sealed class NoOpPreSaveHandler : IRepositoryPreSaveChanges
        {
            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PreSaveChangesAsync(
                RepositoryEntityStates entityState,
                object entity,
                CancellationToken cancellationToken = default)
                => ValueTask.FromResult(entityState);
        }

        private sealed class NoOpPostSaveHandler : IRepositoryPostSaveChanges
        {
            public int Order { get; set; }

            public ValueTask<RepositoryEntityStates> PostSaveChangesAsync(
                RepositoryEntityStates entityState,
                object entity,
                CancellationToken cancellationToken = default)
                => ValueTask.FromResult(entityState);
        }
    }
}
