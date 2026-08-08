using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// Installs the MiCake EF Core write pipeline for a DbContext type through the
    /// EF Core 9+ <see cref="IDbContextOptionsConfiguration{TContext}"/> hook. Registered via
    /// <c>ConfigureDbContext</c>, it composes with the user's <c>AddDbContext</c> (including
    /// pooling) in call order; non-conflicting options are merged. The interceptors are
    /// resolved from the provider and attached explicitly via <c>AddInterceptors</c> —
    /// EF Core does not discover DI-registered interceptor services on its own.
    /// Guard policy is Permissive: writes without an ambient writable UoW pass through
    /// unguarded (native EF semantics); writes inside a UoW are guarded/bound/lifecycle-processed.
    /// </summary>
    internal sealed class MiCakeDbContextOptionsConfigurator<TContext> : IDbContextOptionsConfiguration<TContext>
        where TContext : DbContext
    {
        public void Configure(IServiceProvider serviceProvider, DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMiCake();

            // Resolve the singleton interceptor instances (reused across all contexts of this
            // type, avoiding EF's ManyServiceProvidersCreatedWarning) and attach them
            // explicitly — the only mechanism EF Core supports. When the module has not
            // registered them (e.g. a host wired this configurator manually), fail with
            // guidance instead of a bare DI resolution error.
            var saveChangesInterceptor = serviceProvider.GetService<MiCakeEFCoreInterceptor>();
            var commandInterceptor = serviceProvider.GetService<MiCakeDbCommandInterceptor>();
            if (saveChangesInterceptor == null || commandInterceptor == null)
            {
                throw new InvalidOperationException(
                    $"The MiCake EF Core write pipeline is not registered for DbContext '{typeof(TContext).Name}'. " +
                    "Register the MiCake EF Core module (AddMiCake/AddMiCakeWithDefault with UseEFCore) so it registers " +
                    "the interceptor services and this configurator automatically.");
            }

            optionsBuilder.AddInterceptors(saveChangesInterceptor, commandInterceptor);
        }
    }
}
