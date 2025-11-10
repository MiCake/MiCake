using MiCake.Core.Modularity;
using MiCake.Core.Tests.Modularity.Fakes;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace MiCake.Core.Tests.Modularity
{
    /// <summary>
    /// Tests for the refactored Module System
    /// </summary>
    public class MiCakeModuleSystem_Refactored_Tests
    {
        [Fact]
        public void MiCakeModule_ConfigureServices_ShouldBeCalledSynchronously()
        {
            // Arrange
            var testModule = new TestSynchronousModule();
            var context = new ModuleConfigServiceContext(
                new ServiceCollection(),
                new MiCakeModuleCollection(),
                new MiCakeApplicationOptions());
            
            // Act
            testModule.ConfigureServices(context);
            
            // Assert
            Assert.True(testModule.ConfigureServicesCalled);
        }
        
        [Fact]
        public void MiCakeModule_OnApplicationInitialization_ShouldBeCalledSynchronously()
        {
            // Arrange
            var testModule = new TestSynchronousModule();
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var context = new ModuleInitializationContext(
                serviceProvider,
                new MiCakeModuleCollection(),
                new MiCakeApplicationOptions());
            
            // Act
            testModule.OnApplicationInitialization(context);
            
            // Assert
            Assert.True(testModule.OnApplicationInitializationCalled);
        }
        
        [Fact]
        public void MiCakeModule_OnApplicationShutdown_ShouldBeCalledSynchronously()
        {
            // Arrange
            var testModule = new TestSynchronousModule();
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var context = new ModuleShutdownContext(
                serviceProvider,
                new MiCakeModuleCollection(),
                new MiCakeApplicationOptions());
            
            // Act
            testModule.OnApplicationShutdown(context);
            
            // Assert
            Assert.True(testModule.OnApplicationShutdownCalled);
        }
        
        [Fact]
        public void MiCakeModuleAdvanced_AllLifecycleMethods_ShouldBeCalledSynchronously()
        {
            // Arrange
            var testModule = new TestAdvancedModule();
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var configContext = new ModuleConfigServiceContext(
                services,
                new MiCakeModuleCollection(),
                new MiCakeApplicationOptions());
            var initContext = new ModuleInitializationContext(
                serviceProvider,
                new MiCakeModuleCollection(),
                new MiCakeApplicationOptions());
            var shutdownContext = new ModuleShutdownContext(
                serviceProvider,
                new MiCakeModuleCollection(),
                new MiCakeApplicationOptions());
            
            // Act
            testModule.PreConfigureServices(configContext);
            testModule.ConfigureServices(configContext);
            testModule.PostConfigureServices(configContext);
            testModule.PreInitialization(initContext);
            testModule.OnApplicationInitialization(initContext);
            testModule.PostInitialization(initContext);
            testModule.PreShutdown(shutdownContext);
            testModule.OnApplicationShutdown(shutdownContext);
            
            // Assert
            Assert.True(testModule.PreConfigureServicesCalled);
            Assert.True(testModule.ConfigureServicesCalled);
            Assert.True(testModule.PostConfigureServicesCalled);
            Assert.True(testModule.PreInitializationCalled);
            Assert.True(testModule.OnApplicationInitializationCalled);
            Assert.True(testModule.PostInitializationCalled);
            Assert.True(testModule.PreShutdownCalled);
            Assert.True(testModule.OnApplicationShutdownCalled);
        }
        
        [Fact]
        public void MiCakeRootModule_ShouldBeFrameworkLevel()
        {
            // Arrange & Act
            var rootModule = new MiCakeRootModule();
            
            // Assert
            Assert.True(rootModule.IsFrameworkLevel);
            Assert.Equal("MiCake Root Module - Core functionality for MiCake Framework", rootModule.Description);
        }
        
        [Fact]
        public void ModuleConfigServiceContext_ShouldContainServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var modules = new MiCakeModuleCollection();
            var options = new MiCakeApplicationOptions();
            
            // Act
            var context = new ModuleConfigServiceContext(services, modules, options);
            
            // Assert
            Assert.Equal(services, context.Services);
            Assert.Equal(modules, context.MiCakeModules);
            Assert.Equal(options, context.MiCakeApplicationOptions);
        }
        
        [Fact]
        public void ModuleInitializationContext_ShouldContainServiceProvider()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var modules = new MiCakeModuleCollection();
            var options = new MiCakeApplicationOptions();
            
            // Act
            var context = new ModuleInitializationContext(serviceProvider, modules, options);
            
            // Assert
            Assert.Equal(serviceProvider, context.ServiceProvider);
            Assert.Equal(modules, context.Modules);
            Assert.Equal(options, context.ApplicationOptions);
        }
        
        [Fact]
        public void ModuleShutdownContext_ShouldContainServiceProvider()
        {
            // Arrange
            var serviceProvider = new ServiceCollection().BuildServiceProvider();
            var modules = new MiCakeModuleCollection();
            var options = new MiCakeApplicationOptions();
            
            // Act
            var context = new ModuleShutdownContext(serviceProvider, modules, options);
            
            // Assert
            Assert.Equal(serviceProvider, context.ServiceProvider);
            Assert.Equal(modules, context.Modules);
            Assert.Equal(options, context.ApplicationOptions);
        }
        
        // Test helper modules
        private class TestSynchronousModule : MiCakeModule
        {
            public bool ConfigureServicesCalled { get; private set; }
            public bool OnApplicationInitializationCalled { get; private set; }
            public bool OnApplicationShutdownCalled { get; private set; }
            
            public override void ConfigureServices(ModuleConfigServiceContext context)
            {
                ConfigureServicesCalled = true;
            }
            
            public override void OnApplicationInitialization(ModuleInitializationContext context)
            {
                OnApplicationInitializationCalled = true;
            }
            
            public override void OnApplicationShutdown(ModuleShutdownContext context)
            {
                OnApplicationShutdownCalled = true;
            }
        }
        
        private class TestAdvancedModule : MiCakeModuleAdvanced
        {
            public bool PreConfigureServicesCalled { get; private set; }
            public bool ConfigureServicesCalled { get; private set; }
            public bool PostConfigureServicesCalled { get; private set; }
            public bool PreInitializationCalled { get; private set; }
            public bool OnApplicationInitializationCalled { get; private set; }
            public bool PostInitializationCalled { get; private set; }
            public bool PreShutdownCalled { get; private set; }
            public bool OnApplicationShutdownCalled { get; private set; }
            
            public override void PreConfigureServices(ModuleConfigServiceContext context)
            {
                PreConfigureServicesCalled = true;
            }
            
            public override void ConfigureServices(ModuleConfigServiceContext context)
            {
                ConfigureServicesCalled = true;
            }
            
            public override void PostConfigureServices(ModuleConfigServiceContext context)
            {
                PostConfigureServicesCalled = true;
            }
            
            public override void PreInitialization(ModuleInitializationContext context)
            {
                PreInitializationCalled = true;
            }
            
            public override void OnApplicationInitialization(ModuleInitializationContext context)
            {
                OnApplicationInitializationCalled = true;
            }
            
            public override void PostInitialization(ModuleInitializationContext context)
            {
                PostInitializationCalled = true;
            }
            
            public override void PreShutdown(ModuleShutdownContext context)
            {
                PreShutdownCalled = true;
            }
            
            public override void OnApplicationShutdown(ModuleShutdownContext context)
            {
                OnApplicationShutdownCalled = true;
            }
        }
    }
}
