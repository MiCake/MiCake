using MiCake.IntegrationTests.Fakes;
using MiCake.IntegrationTests.Infrastructure;
using MiCake.DDD.Uow;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MiCake.IntegrationTests
{
    /// <summary>
    /// Integration tests for Dependency Injection and service lifetime management
    /// </summary>
    public class DependencyInjection_IntegrationTests : IntegrationTestBase
    {
        [Fact]
        public void DI_TransientService_ShouldCreateNewInstanceEachTime()
        {
            // Arrange
            var scope = ServiceProvider.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            // Act - Get transient service twice from same scope
            var transient1 = serviceProvider.GetService<ITransientTestService>();
            var transient2 = serviceProvider.GetService<ITransientTestService>();

            // Assert - Should be different instances
            Assert.NotNull(transient1);
            Assert.NotNull(transient2);
            Assert.NotEqual(transient1.GetId(), transient2.GetId());
        }

        [Fact]
        public void DI_ScopedService_ShouldBeSameWithinScope()
        {
            // Arrange
            var scope1 = ServiceProvider.CreateScope();
            var serviceProvider1 = scope1.ServiceProvider;

            var scope2 = ServiceProvider.CreateScope();
            var serviceProvider2 = scope2.ServiceProvider;

            // Act - Get scoped service multiple times within same scope
            var scoped1a = serviceProvider1.GetService<IScopedTestService>();
            var scoped1b = serviceProvider1.GetService<IScopedTestService>();

            // Act - Get scoped service in different scope
            var scoped2 = serviceProvider2.GetService<IScopedTestService>();

            // Assert - Same scope should return same instance
            Assert.NotNull(scoped1a);
            Assert.NotNull(scoped1b);
            Assert.Equal(scoped1a.GetId(), scoped1b.GetId());

            // Assert - Different scopes should return different instances
            Assert.NotEqual(scoped1a.GetId(), scoped2.GetId());
        }

        [Fact]
        public void DI_SingletonService_ShouldBeSameDuringApplicationLifetime()
        {
            // Arrange
            var scope1 = ServiceProvider.CreateScope();
            var serviceProvider1 = scope1.ServiceProvider;

            var scope2 = ServiceProvider.CreateScope();
            var serviceProvider2 = scope2.ServiceProvider;

            // Act - Get singleton service from different scopes
            var singleton1 = serviceProvider1.GetService<ISingletonTestService>();
            var singleton2 = serviceProvider2.GetService<ISingletonTestService>();

            // Assert - All should return same singleton instance
            Assert.NotNull(singleton1);
            Assert.NotNull(singleton2);
            Assert.Equal(singleton1.GetId(), singleton2.GetId());
        }

        [Fact]
        public void DI_MixedLifetimes_ShouldWorkCorrectly()
        {
            // Arrange
            var scope1 = ServiceProvider.CreateScope();
            var scope1Provider = scope1.ServiceProvider;

            var scope2 = ServiceProvider.CreateScope();
            var scope2Provider = scope2.ServiceProvider;

            // Act - Get all three service types from both scopes
            var transient1 = scope1Provider.GetService<ITransientTestService>();
            var scoped1 = scope1Provider.GetService<IScopedTestService>();
            var singleton1 = scope1Provider.GetService<ISingletonTestService>();

            var transient2 = scope1Provider.GetService<ITransientTestService>();
            var scoped2 = scope1Provider.GetService<IScopedTestService>();
            var singleton2 = scope1Provider.GetService<ISingletonTestService>();

            var transient3 = scope2Provider.GetService<ITransientTestService>();
            var scoped3 = scope2Provider.GetService<IScopedTestService>();
            var singleton3 = scope2Provider.GetService<ISingletonTestService>();

            // Assert
            // Transient: all different
            Assert.NotEqual(transient1.GetId(), transient2.GetId());
            Assert.NotEqual(transient1.GetId(), transient3.GetId());
            Assert.NotEqual(transient2.GetId(), transient3.GetId());

            // Scoped: same within scope, different across scopes
            Assert.Equal(scoped1.GetId(), scoped2.GetId());
            Assert.NotEqual(scoped1.GetId(), scoped3.GetId());

            // Singleton: all same
            Assert.Equal(singleton1.GetId(), singleton2.GetId());
            Assert.Equal(singleton1.GetId(), singleton3.GetId());
        }

        [Fact]
        public void DI_ServiceResolution_ShouldWorkForFrameworkServices()
        {
            // Arrange & Act
            var uowManager = ServiceProvider.GetService<IUnitOfWorkManager>();

            // Assert - Framework services should be available
            Assert.NotNull(uowManager);
        }

        [Fact]
        public void DI_MultipleScopes_ShouldBeIndependent()
        {
            // Arrange - Create multiple scopes
            using (var scope1 = ServiceProvider.CreateScope())
            using (var scope2 = ServiceProvider.CreateScope())
            using (var scope3 = ServiceProvider.CreateScope())
            {
                // Act - Get scoped services from each scope
                var service1 = scope1.ServiceProvider.GetService<IScopedTestService>();
                var service2 = scope2.ServiceProvider.GetService<IScopedTestService>();
                var service3 = scope3.ServiceProvider.GetService<IScopedTestService>();

                // Assert - All should be different instances
                Assert.NotNull(service1);
                Assert.NotNull(service2);
                Assert.NotNull(service3);
                Assert.NotEqual(service1.GetId(), service2.GetId());
                Assert.NotEqual(service2.GetId(), service3.GetId());
                Assert.NotEqual(service1.GetId(), service3.GetId());
            }
        }

        [Fact]
        public void DI_ScopeDisposal_ShouldNotAffectParentProvider()
        {
            // Arrange
            var scope1 = ServiceProvider.CreateScope();
            var singleton1 = scope1.ServiceProvider.GetService<ISingletonTestService>();

            // Act - Dispose scope
            scope1.Dispose();

            // Act - Create new scope and resolve singleton
            var scope2 = ServiceProvider.CreateScope();
            var singleton2 = scope2.ServiceProvider.GetService<ISingletonTestService>();

            // Assert - Singleton should be same even after scope disposal
            Assert.Equal(singleton1.GetId(), singleton2.GetId());

            scope2.Dispose();
        }

        [Fact]
        public void DI_TransientInScoped_ShouldBeCreatedFresh()
        {
            // Arrange
            using (var scope = ServiceProvider.CreateScope())
            {
                // Act - Get transient multiple times in same scope
                var transient1 = scope.ServiceProvider.GetService<ITransientTestService>();
                var transient2 = scope.ServiceProvider.GetService<ITransientTestService>();
                var transient3 = scope.ServiceProvider.GetService<ITransientTestService>();

                // Assert - All should be different
                Assert.NotEqual(transient1.GetId(), transient2.GetId());
                Assert.NotEqual(transient2.GetId(), transient3.GetId());
                Assert.NotEqual(transient1.GetId(), transient3.GetId());
            }
        }

        [Fact]
        public void DI_ServiceProvider_ShouldBeConsistent()
        {
            // Arrange & Act
            var service1 = ServiceProvider.GetService<ISingletonTestService>();
            var service2 = ServiceProvider.GetService<ISingletonTestService>();

            // Assert - Root singleton should always be same
            Assert.NotNull(service1);
            Assert.NotNull(service2);
            Assert.Same(service1, service2);
        }

        [Fact]
        public void DI_ScopedServiceLifetime_ShouldFollowPattern()
        {
            // Arrange
            var ids = new List<string>();

            using (var scope1 = ServiceProvider.CreateScope())
            {
                var service1a = scope1.ServiceProvider.GetService<IScopedTestService>();
                var service1b = scope1.ServiceProvider.GetService<IScopedTestService>();
                ids.Add(service1a.GetId());
                ids.Add(service1b.GetId());

                // Should be same
                Assert.Equal(ids[0], ids[1]);
            }

            using (var scope2 = ServiceProvider.CreateScope())
            {
                var service2 = scope2.ServiceProvider.GetService<IScopedTestService>();
                ids.Add(service2.GetId());

                // Should be different from scope1
                Assert.NotEqual(ids[0], ids[2]);
            }
        }
    }
}
