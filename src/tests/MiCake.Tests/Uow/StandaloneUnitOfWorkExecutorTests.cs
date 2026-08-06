using MiCake.DDD.Uow;
using MiCake.DDD.Uow.Exceptions;
using MiCake.DDD.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Unit tests for the provider-neutral standalone unit of work executor.
    /// </summary>
    public class StandaloneUnitOfWorkExecutorTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AmbientUnitOfWorkAccessor _ambientAccessor;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly StandaloneUnitOfWorkExecutor _executor;

        public StandaloneUnitOfWorkExecutorTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<AmbientUnitOfWorkAccessor>();
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            services.AddScoped<IStandaloneUnitOfWorkExecutor, StandaloneUnitOfWorkExecutor>();
            _serviceProvider = services.BuildServiceProvider();

            _ambientAccessor = _serviceProvider.GetRequiredService<AmbientUnitOfWorkAccessor>();
            _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
            _executor = new StandaloneUnitOfWorkExecutor(
                _ambientAccessor,
                _scopeFactory,
                _serviceProvider.GetRequiredService<ILogger<StandaloneUnitOfWorkExecutor>>());
        }

        [Fact]
        public async Task ExecuteAsync_WithExistingAmbientUow_ShouldReject()
        {
            var manager = new UnitOfWorkManager(
                _serviceProvider,
                _ambientAccessor,
                _scopeFactory,
                _serviceProvider.GetRequiredService<ILogger<UnitOfWorkManager>>());
            using var outer = await manager.BeginAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _executor.ExecuteAsync((sp, ct) => Task.CompletedTask));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCommitAndRestoreNoAmbient()
        {
            TestUowResource? resource = null;

            await _executor.ExecuteAsync(async (sp, ct) =>
            {
                var innerManager = sp.GetRequiredService<IUnitOfWorkManager>();
                var uow = innerManager.Current!;
                Assert.Null(uow.Parent);
                Assert.NotNull(uow);
                resource = new TestUowResource();
                ((IUnitOfWorkInternal)uow).RegisterResource(resource);
            });

            Assert.NotNull(resource);
            Assert.Equal(1, resource!.CommitCount);
            Assert.Equal(0, resource.RollbackCount);
            Assert.Null(_ambientAccessor.Current);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnResultAndCommit()
        {
            TestUowResource? resource = null;

            var result = await _executor.ExecuteAsync(async (sp, ct) =>
            {
                var uow = sp.GetRequiredService<IUnitOfWorkManager>().Current!;
                resource = new TestUowResource();
                ((IUnitOfWorkInternal)uow).RegisterResource(resource);
                return "done";
            });

            Assert.Equal("done", result);
            Assert.Equal(1, resource!.CommitCount);
            Assert.Null(_ambientAccessor.Current);
        }

        [Fact]
        public async Task ExecuteAsync_OperationFailure_ShouldRollback()
        {
            TestUowResource? resource = null;

            await Assert.ThrowsAsync<InvalidOperationException>(() => _executor.ExecuteAsync(async (sp, ct) =>
            {
                var uow = sp.GetRequiredService<IUnitOfWorkManager>().Current!;
                resource = new TestUowResource();
                ((IUnitOfWorkInternal)uow).RegisterResource(resource);
                throw new InvalidOperationException("boom");
            }));

            Assert.NotNull(resource);
            Assert.Equal(1, resource!.RollbackCount);
            Assert.Equal(0, resource.CommitCount);
            Assert.Null(_ambientAccessor.Current);
        }

        [Fact]
        public async Task ExecuteAsync_OperationAndRollbackFailure_ShouldThrowBoundaryException()
        {
            var ex = await Assert.ThrowsAsync<UnitOfWorkBoundaryException>(() => _executor.ExecuteAsync(async (sp, ct) =>
            {
                var uow = sp.GetRequiredService<IUnitOfWorkManager>().Current!;
                var resource = new TestUowResource { RollbackException = new InvalidOperationException("rollback failed") };
                ((IUnitOfWorkInternal)uow).RegisterResource(resource);
                throw new InvalidOperationException("boom");
            }));

            Assert.IsType<InvalidOperationException>(ex.PrimaryException);
            Assert.Single(ex.RollbackExceptions);
            Assert.Null(_ambientAccessor.Current);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldDisposeUnitOfWorkAfterCompletion()
        {
            TestUowResource? resource = null;

            await _executor.ExecuteAsync(async (sp, ct) =>
            {
                var uow = sp.GetRequiredService<IUnitOfWorkManager>().Current!;
                resource = new TestUowResource();
                ((IUnitOfWorkInternal)uow).RegisterResource(resource);
            });

            Assert.Equal(1, resource!.DisposeCount);
        }
    }
}
