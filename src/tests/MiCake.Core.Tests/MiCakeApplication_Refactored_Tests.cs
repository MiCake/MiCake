using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Reflection;
using Xunit;

namespace MiCake.Core.Tests
{
    /// <summary>
    /// Tests for the refactored MiCake Application and Module System
    /// </summary>
    public class MiCakeApplication_Refactored_Tests
    {
        [Fact]
        public void MiCakeBuilder_Build_ShouldDiscoverModules()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            
            var options = new MiCakeApplicationOptions();
            var builder = new MiCakeBuilder(services, typeof(MiCakeCoreTestModule), options);
            
            // Act
            builder.Build();
            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetRequiredService<IMiCakeApplication>();
            
            // Assert
            Assert.NotNull(app);
            Assert.NotNull(app.ModuleContext);
            Assert.NotNull(app.ModuleContext.MiCakeModules);
        }
        
        [Fact]
        public void MiCakeBuilder_Build_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            
            var options = new MiCakeApplicationOptions();
            var builder = new MiCakeBuilder(services, typeof(MiCakeCoreTestModule), options);
            
            // Act
            builder.Build();
            var serviceProvider = services.BuildServiceProvider();
            
            // Assert - IMiCakeApplication should be registered
            var app = serviceProvider.GetService<IMiCakeApplication>();
            Assert.NotNull(app);
            
            // Assert - IMiCakeModuleContext should be registered
            var moduleContext = serviceProvider.GetService<IMiCakeModuleContext>();
            Assert.NotNull(moduleContext);
        }
        
        [Fact]
        public void MiCakeApplication_Start_ShouldExecuteSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            
            var options = new MiCakeApplicationOptions();
            var builder = new MiCakeBuilder(services, typeof(MiCakeCoreTestModule), options);
            builder.Build();
            
            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetRequiredService<IMiCakeApplication>();
            
            // Act & Assert - Should not throw
            app.Start();
        }
        
        [Fact]
        public void MiCakeApplication_ShutDown_ShouldExecuteSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            
            var options = new MiCakeApplicationOptions();
            var builder = new MiCakeBuilder(services, typeof(MiCakeCoreTestModule), options);
            builder.Build();
            
            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetRequiredService<IMiCakeApplication>();
            app.Start();
            
            // Act & Assert - Should not throw
            app.ShutDown();
        }
        
        [Fact]
        public void MiCakeApplication_Start_CalledTwice_ShouldThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            
            var options = new MiCakeApplicationOptions();
            var builder = new MiCakeBuilder(services, typeof(MiCakeCoreTestModule), options);
            builder.Build();
            
            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetRequiredService<IMiCakeApplication>();
            app.Start();
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => app.Start());
        }
        
        [Fact]
        public void MiCakeApplication_ShutDown_CalledTwice_ShouldThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            
            var options = new MiCakeApplicationOptions();
            var builder = new MiCakeBuilder(services, typeof(MiCakeCoreTestModule), options);
            builder.Build();
            
            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetRequiredService<IMiCakeApplication>();
            app.Start();
            app.ShutDown();
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => app.ShutDown());
        }
        
        [Fact]
        public void MiCakeApplicationOptions_Apply_ShouldCopyValues()
        {
            // Arrange
            Assembly[] assemblies = { GetType().Assembly };
            var sourceOptions = new MiCakeApplicationOptions
            {
                DomainLayerAssemblies = assemblies
            };
            
            var targetOptions = new MiCakeApplicationOptions();
            
            // Act
            targetOptions.Apply(sourceOptions);
            
            // Assert
            Assert.Equal(assemblies, targetOptions.DomainLayerAssemblies);
            Assert.NotNull(targetOptions.BuildTimeData);
        }
        
        [Fact]
        public void MiCakeBuilder_WithOptions_ShouldConfigureApplication()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            
            Assembly[] assemblies = { GetType().Assembly };
            var options = new MiCakeApplicationOptions
            {
                DomainLayerAssemblies = assemblies
            };
            
            var builder = new MiCakeBuilder(services, typeof(MiCakeCoreTestModule), options);
            
            // Act
            builder.Build();
            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetRequiredService<IMiCakeApplication>();
            
            // Assert
            Assert.NotNull(app.ApplicationOptions);
            Assert.Equal(assemblies, app.ApplicationOptions.DomainLayerAssemblies);
        }
        
        [Fact]
        public void MiCakeApplication_Dispose_ShouldShutdownApplication()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            
            var options = new MiCakeApplicationOptions();
            var builder = new MiCakeBuilder(services, typeof(MiCakeCoreTestModule), options);
            builder.Build();
            
            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetRequiredService<IMiCakeApplication>();
            app.Start();
            
            // Act & Assert - Should not throw
            app.Dispose();
            
            // Verify shutdown was called by attempting to shutdown again
            Assert.Throws<InvalidOperationException>(() => app.ShutDown());
        }
    }
}
