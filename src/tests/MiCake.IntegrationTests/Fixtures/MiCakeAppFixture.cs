using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using MiCake.Core;

namespace MiCake.IntegrationTests.Fixtures
{
    /// <summary>
    /// A test fixture that centralizes MiCake application start/shutdown for integration tests.
    /// Tests call <see cref="CreateServiceProvider(Action{IServiceCollection})"/> to build a
    /// dedicated service provider, MiCake app will be started automatically. Call
    /// <see cref="ReleaseServiceProvider(IServiceProvider)"/> to shutdown and dispose the
    /// provider, or let the fixture cleanup at collection-level disposal.
    /// </summary>
    public class MiCakeAppFixture : IDisposable
    {
        private readonly List<IMiCakeApplication> _apps = new();
        private readonly List<IDisposable> _providers = new();

        public ServiceProvider CreateServiceProvider(Action<IServiceCollection> configureServices)
        {
            var services = new ServiceCollection();

            // Let the test configure services (AddDbContext, logging etc.)
            configureServices?.Invoke(services);

            // Build a provider for this test
            var provider = services.BuildServiceProvider();

            // Start MiCake application automatically if present
            try
            {
                var app = provider.GetService<IMiCakeApplication>();
                if (app != null)
                {
                    app.Start();
                    _apps.Add(app);
                }
            }
            catch
            {
                // If start fails, dispose provider and rethrow to the caller
                (provider as IDisposable)?.Dispose();
                throw;
            }

            _providers.Add(provider);
            return provider;
        }

        public void ReleaseServiceProvider(ServiceProvider provider)
        {
            if (provider == null)
                return;

            // If provider contains a MiCakeApplication, ensure it's shutdown.
            try
            {
                var app = provider.GetService<IMiCakeApplication>();
                if (app != null)
                {
                    try { app.ShutDown(); } catch { }
                    _apps.Remove(app);
                }
            }
            catch { }

            // Dispose the provider
            try { (provider as IDisposable)?.Dispose(); } catch { }
            _providers.Remove(provider);
        }

        public void Dispose()
        {
            // Ensure all apps are shutdown and providers disposed
            foreach (var app in _apps)
            {
                try { app.ShutDown(); } catch { }
            }

            foreach (var provider in _providers)
            {
                try { provider.Dispose(); } catch { }
            }

            _apps.Clear();
            _providers.Clear();

            // Clear any static helpers to avoid cross-test interference
            MiCake.EntityFrameworkCore.Internal.MiCakeInterceptorFactoryHelper.Reset();
        }
    }
}
