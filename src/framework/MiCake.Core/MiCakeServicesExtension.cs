using System;
using MiCake.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.Core;

/// <summary>
/// Provides extension methods for integrating MiCake framework services into the dependency injection container.
/// </summary>
public static class MiCakeServicesExtension
{
    /// <summary>
    /// Registers MiCake core services with the specified entry module type.
    /// </summary>
    /// <typeparam name="TEntryModule">The type of the entry module that inherits from <see cref="MiCakeModule"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>An <see cref="IMiCakeBuilder"/> instance for further configuration.</returns>
    public static IMiCakeBuilder AddMiCake<TEntryModule>(this IServiceCollection services)
        where TEntryModule : MiCakeModule
    {
        return AddMiCake<TEntryModule>(services, null);
    }

    /// <summary>
    /// Registers MiCake core services with the specified entry module type and configuration options.
    /// </summary>
    /// <typeparam name="TEntryModule">The type of the entry module that inherits from <see cref="MiCakeModule"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configOptions">An optional action to configure <see cref="MiCakeApplicationOptions"/>.</param>
    /// <returns>An <see cref="IMiCakeBuilder"/> instance for further configuration.</returns>
    public static IMiCakeBuilder AddMiCake<TEntryModule>(
        this IServiceCollection services,
        Action<MiCakeApplicationOptions>? configOptions)
         where TEntryModule : MiCakeModule
    {
        return AddMiCake(services, typeof(TEntryModule), configOptions);
    }

    /// <summary>
    /// Registers MiCake core services with the specified entry module type and configuration options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="entryModule">The <see cref="Type"/> of the entry module that inherits from <see cref="MiCakeModule"/>.</param>
    /// <param name="configOptions">An optional action to configure <see cref="MiCakeApplicationOptions"/>.</param>
    /// <returns>An <see cref="IMiCakeBuilder"/> instance for further configuration.</returns>
    public static IMiCakeBuilder AddMiCake(
           this IServiceCollection services,
           Type entryModule,
           Action<MiCakeApplicationOptions>? configOptions)
    {
        MiCakeApplicationOptions options = new();
        configOptions?.Invoke(options);

        var builder = new DefaultMiCakeBuilderProvider(services, entryModule, options).GetMiCakeBuilder();
        return builder;
    }
}
