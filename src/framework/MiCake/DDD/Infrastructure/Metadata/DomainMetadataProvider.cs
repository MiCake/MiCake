using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.Util.Reflection;
using MiCake.DDD.Domain.Helper;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.DDD.Infrastructure.Metadata
{
    /// <summary>
    /// Provides domain metadata by scanning assemblies for domain objects.
    /// Simplified design with direct scanning - no complex provider pattern.
    /// </summary>
    public interface IDomainMetadataProvider
    {
        /// <summary>
        /// Gets the domain metadata
        /// </summary>
        DomainMetadata GetDomainMetadata();
    }

    /// <summary>
    /// Default implementation that scans assemblies for domain objects
    /// </summary>
    internal class DomainMetadataProvider : IDomainMetadataProvider
    {
        private readonly Assembly[] _assemblies;
        private DomainMetadata? _cachedMetadata;
        private readonly object _lock = new();

        public DomainMetadataProvider(
            IMiCakeModuleContext moduleContext,
            IOptions<MiCakeApplicationOptions> appOptions)
        {
            System.ArgumentNullException.ThrowIfNull(moduleContext);
            System.ArgumentNullException.ThrowIfNull(appOptions);

            var userModules = new MiCakeModuleCollection();
            foreach (var descriptor in moduleContext.MiCakeModules.Where(s => !s.Instance.IsFrameworkLevel))
            {
                userModules.Add(descriptor);
            }

            _assemblies = (appOptions.Value.DomainLayerAssemblies != null && appOptions.Value.DomainLayerAssemblies.Length > 0)
                ? appOptions.Value.DomainLayerAssemblies
                : DiscoverDomainAssemblies(userModules);
        }

        public DomainMetadata GetDomainMetadata()
        {
            if (_cachedMetadata != null)
                return _cachedMetadata;

            lock (_lock)
            {
                if (_cachedMetadata != null)
                    return _cachedMetadata;

                _cachedMetadata = ScanAssemblies();
                return _cachedMetadata;
            }
        }

        private DomainMetadata ScanAssemblies()
        {
            var aggregates = new List<AggregateRootDescriptor>();
            var entities = new List<EntityDescriptor>();
            var valueObjects = new List<ValueObjectDescriptor>();

            // Get all concrete types from domain assemblies
            var allTypes = _assemblies
                .SelectMany(asm => asm.GetTypes())
                .Where(t => TypeHelper.IsConcrete(t) && DomainTypeHelper.IsDomainObject(t));

            foreach (var type in allTypes)
            {
                if (DomainTypeHelper.IsAggregateRoot(type))
                {
                    var keyType = EntityHelper.FindPrimaryKeyType(type);
                    if (keyType != null)
                    {
                        aggregates.Add(new AggregateRootDescriptor(type, keyType));
                    }
                }
                else if (DomainTypeHelper.IsEntity(type))
                {
                    var keyType = EntityHelper.FindPrimaryKeyType(type);
                    if (keyType != null)
                    {
                        entities.Add(new EntityDescriptor(type, keyType));
                    }
                }
                else if (DomainTypeHelper.IsValueObject(type))
                {
                    valueObjects.Add(new ValueObjectDescriptor(type));
                }
            }

            return new DomainMetadata(_assemblies, aggregates, entities, valueObjects);
        }

        private static Assembly[] DiscoverDomainAssemblies(IMiCakeModuleCollection modules)
        {
            return modules.GetAssemblies(false)
                .Where(asm => asm.GetTypes().Any(t => DomainTypeHelper.IsDomainObject(t)))
                .ToArray();
        }
    }
}
