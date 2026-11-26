using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace MiCake.EntityFrameworkCore.Internal
{
    /// <summary>
    /// EF Core options extension that registers the per-DbContext save-operation state
    /// accessor as an internal service and carries the host save-cycle limit.
    /// </summary>
    internal sealed class MiCakeSaveOperationOptionsExtension : IDbContextOptionsExtension
    {
        public int MaxSaveCycles { get; init; } = 16;

        public void ApplyServices(IServiceCollection services)
        {
            services.AddScoped<SaveOperationStateAccessor>();
        }

        public void Validate(IDbContextOptions options)
        {
            if (MaxSaveCycles < 1)
            {
                throw new InvalidOperationException(
                    $"MiCake SaveOperation MaxSaveCycles must be at least 1, but was {MaxSaveCycles}.");
            }
        }

        public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension)
                : base(extension)
            {
            }

            public override bool IsDatabaseProvider => false;

            public override string LogFragment => "MiCakeSaveOperation";

            public override int GetServiceProviderHashCode() => 0;

            public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
                => other is ExtensionInfo;

            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
                => debugInfo["MiCake:MaxSaveCycles"] = ((MiCakeSaveOperationOptionsExtension)Extension).MaxSaveCycles.ToString();
        }
    }
}
