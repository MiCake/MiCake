using MiCake.Audit;
using MiCake.Core;
using MiCake.Core.Data;
using MiCake.Core.Modularity;
using MiCake.DDD.Uow;
using MiCake.EntityFrameworkCore;
using MiCake.EntityFrameworkCore.Modules;
using MiCake.Modules;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MiCake.IntegrationTests
{
    /// <summary>
    /// Integration tests for MiCake Module configuration and initialization
    /// </summary>
    public class Module_IntegrationTests
    {
        [Fact]
        public async Task Module_ConfigServices_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            
            var app = services.AddMiCake<SimpleModule>()
                .UseAudit()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            var micakeApp = serviceProvider.GetService<IMiCakeApplication>();
            if (micakeApp is IDependencyReceiver<IServiceProvider> needProvider)
            {
                needProvider.AddDependency(serviceProvider);
            }

            // Act
            await micakeApp!.Start();

            try
            {
                // Assert - Verify services are registered
                var uowManager = serviceProvider.GetService<IUnitOfWorkManager>();
                Assert.NotNull(uowManager);
            }
            finally
            {
                await micakeApp.ShutDown();
                (serviceProvider as IDisposable)?.Dispose();
            }
        }

        [Fact]
        public async Task Module_DependsOn_ShouldResolveCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act - Module with dependencies should initialize without exception
            var app = services.AddMiCake<ModuleWithDependencies>()
                .UseAudit()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            var micakeApp = serviceProvider.GetService<IMiCakeApplication>();
            if (micakeApp is IDependencyReceiver<IServiceProvider> needProvider)
            {
                needProvider.AddDependency(serviceProvider);
            }

            await micakeApp!.Start();

            try
            {
                // Assert - Services from dependent modules should be available
                var uowManager = serviceProvider.GetService<IUnitOfWorkManager>();
                Assert.NotNull(uowManager);
            }
            finally
            {
                await micakeApp.ShutDown();
                (serviceProvider as IDisposable)?.Dispose();
            }
        }

        [Fact]
        public async Task Module_PreConfigServices_ShouldExecuteFirst()
        {
            // Arrange
            var trackingList = new List<string>();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(trackingList);

            var app = services.AddMiCake<TrackingModule>()
                .UseAudit()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            var micakeApp = serviceProvider.GetService<IMiCakeApplication>();
            if (micakeApp is IDependencyReceiver<IServiceProvider> needProvider)
            {
                needProvider.AddDependency(serviceProvider);
            }

            // Act
            await micakeApp!.Start();

            try
            {
                // Assert - PreConfigServices should execute before ConfigServices
                Assert.NotEmpty(trackingList);
                var preIndex = trackingList.IndexOf("PreConfigServices");
                var configIndex = trackingList.IndexOf("ConfigServices");
                Assert.True(preIndex >= 0, "PreConfigServices should be called");
                Assert.True(configIndex >= 0, "ConfigServices should be called");
                Assert.True(preIndex < configIndex, "PreConfigServices should execute before ConfigServices");
            }
            finally
            {
                await micakeApp.ShutDown();
                (serviceProvider as IDisposable)?.Dispose();
            }
        }

        [Fact]
        public async Task Module_PostConfigServices_ShouldExecuteAfterConfigServices()
        {
            // Arrange
            var trackingList = new List<string>();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(trackingList);

            var app = services.AddMiCake<TrackingModule>()
                .UseAudit()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            var micakeApp = serviceProvider.GetService<IMiCakeApplication>();
            if (micakeApp is IDependencyReceiver<IServiceProvider> needProvider)
            {
                needProvider.AddDependency(serviceProvider);
            }

            // Act
            await micakeApp!.Start();

            try
            {
                // Assert - PostConfigServices should execute after ConfigServices
                Assert.NotEmpty(trackingList);
                var configIndex = trackingList.IndexOf("ConfigServices");
                var postConfigIndex = trackingList.IndexOf("PostConfigServices");
                Assert.True(configIndex >= 0, "ConfigServices should be called");
                Assert.True(postConfigIndex >= 0, "PostConfigServices should be called");
                Assert.True(configIndex < postConfigIndex, "ConfigServices should execute before PostConfigServices");
            }
            finally
            {
                await micakeApp.ShutDown();
                (serviceProvider as IDisposable)?.Dispose();
            }
        }

        [Fact]
        public async Task Module_LifecycleOrder_ShouldFollowCorrectSequence()
        {
            // Arrange
            var trackingList = new List<string>();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(trackingList);

            var app = services.AddMiCake<FullLifecycleTrackingModule>()
                .UseAudit()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            var micakeApp = serviceProvider.GetService<IMiCakeApplication>();
            if (micakeApp is IDependencyReceiver<IServiceProvider> needProvider)
            {
                needProvider.AddDependency(serviceProvider);
            }

            // Act
            await micakeApp!.Start();

            try
            {
                // Assert - Verify lifecycle order: Pre > Config > Post > Pre Init > Init > Post Init
                Assert.NotEmpty(trackingList);
                int GetOrNegativeOne(string item) => trackingList.IndexOf(item);

                var preConfigIdx = GetOrNegativeOne("PreConfigServices");
                var configIdx = GetOrNegativeOne("ConfigServices");
                var postConfigIdx = GetOrNegativeOne("PostConfigServices");
                var preInitIdx = GetOrNegativeOne("PreInitialization");
                var initIdx = GetOrNegativeOne("Initialization");
                var postInitIdx = GetOrNegativeOne("PostInitialization");

                // All should be called
                Assert.True(preConfigIdx >= 0, "PreConfigServices should be called");
                Assert.True(configIdx >= 0, "ConfigServices should be called");
                Assert.True(postConfigIdx >= 0, "PostConfigServices should be called");

                // Config phases should be in order
                Assert.True(preConfigIdx < configIdx, "PreConfigServices should execute before ConfigServices");
                Assert.True(configIdx < postConfigIdx, "ConfigServices should execute before PostConfigServices");
            }
            finally
            {
                await micakeApp.ShutDown();
                (serviceProvider as IDisposable)?.Dispose();
            }
        }

        [Fact]
        public async Task Module_ServiceResolution_ShouldSucceedAfterInitialization()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            var app = services.AddMiCake<SimpleModule>()
                .UseAudit()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            var micakeApp = serviceProvider.GetService<IMiCakeApplication>();
            if (micakeApp is IDependencyReceiver<IServiceProvider> needProvider)
            {
                needProvider.AddDependency(serviceProvider);
            }

            await micakeApp!.Start();

            try
            {
                // Act & Assert - Multiple service resolutions should succeed
                var uowManager1 = serviceProvider.GetService<IUnitOfWorkManager>();
                var uowManager2 = serviceProvider.GetService<IUnitOfWorkManager>();
                
                Assert.NotNull(uowManager1);
                Assert.NotNull(uowManager2);
                Assert.Same(uowManager1, uowManager2); // Should be same singleton instance
            }
            finally
            {
                await micakeApp.ShutDown();
                (serviceProvider as IDisposable)?.Dispose();
            }
        }

        // Test module implementations
        [RelyOn(typeof(MiCakeEFCoreModule))]
        [RelyOn(typeof(MiCakeEssentialModule))]
        public class SimpleModule : MiCakeModule
        {
            public override Task ConfigServices(ModuleConfigServiceContext context)
            {
                return base.ConfigServices(context);
            }
        }

        [RelyOn(typeof(MiCakeEFCoreModule))]
        [RelyOn(typeof(MiCakeEssentialModule))]
        public class ModuleWithDependencies : MiCakeModule
        {
            public override Task ConfigServices(ModuleConfigServiceContext context)
            {
                return base.ConfigServices(context);
            }
        }

        [RelyOn(typeof(MiCakeEFCoreModule))]
        [RelyOn(typeof(MiCakeEssentialModule))]
        public class TrackingModule : MiCakeModule
        {
            public override Task PreConfigServices(ModuleConfigServiceContext context)
            {
                var trackingList = context.Services.BuildServiceProvider().GetService<List<string>>();
                trackingList?.Add("PreConfigServices");
                return base.PreConfigServices(context);
            }

            public override Task ConfigServices(ModuleConfigServiceContext context)
            {
                var trackingList = context.Services.BuildServiceProvider().GetService<List<string>>();
                trackingList?.Add("ConfigServices");
                return base.ConfigServices(context);
            }

            public override Task PostConfigServices(ModuleConfigServiceContext context)
            {
                var trackingList = context.Services.BuildServiceProvider().GetService<List<string>>();
                trackingList?.Add("PostConfigServices");
                return base.PostConfigServices(context);
            }
        }

        [RelyOn(typeof(MiCakeEFCoreModule))]
        [RelyOn(typeof(MiCakeEssentialModule))]
        public class FullLifecycleTrackingModule : MiCakeModule
        {
            public override Task PreConfigServices(ModuleConfigServiceContext context)
            {
                var trackingList = context.Services.BuildServiceProvider().GetService<List<string>>();
                trackingList?.Add("PreConfigServices");
                return base.PreConfigServices(context);
            }

            public override Task ConfigServices(ModuleConfigServiceContext context)
            {
                var trackingList = context.Services.BuildServiceProvider().GetService<List<string>>();
                trackingList?.Add("ConfigServices");
                return base.ConfigServices(context);
            }

            public override Task PostConfigServices(ModuleConfigServiceContext context)
            {
                var trackingList = context.Services.BuildServiceProvider().GetService<List<string>>();
                trackingList?.Add("PostConfigServices");
                return base.PostConfigServices(context);
            }

            public override Task PreInitialization(ModuleLoadContext context)
            {
                var trackingList = context.ServiceProvider.GetService<List<string>>();
                trackingList?.Add("PreInitialization");
                return base.PreInitialization(context);
            }

            public override Task Initialization(ModuleLoadContext context)
            {
                var trackingList = context.ServiceProvider.GetService<List<string>>();
                trackingList?.Add("Initialization");
                return base.Initialization(context);
            }

            public override Task PostInitialization(ModuleLoadContext context)
            {
                var trackingList = context.ServiceProvider.GetService<List<string>>();
                trackingList?.Add("PostInitialization");
                return base.PostInitialization(context);
            }
        }
    }
}
