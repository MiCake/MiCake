using MiCake.Core.Data;
using MiCake.Core.Util;
using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Extensions.Store.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// This class is used to automatically call the mapping configuration method of persistent object.
    /// </summary>
    internal class PersistentObjectActivator : IPersistentObjectActivator, IDisposable
    {
        private DomainMetadata _domainMetadata;
        private IPersistentObjectMapper _persistentObjectMapper;

        public PersistentObjectActivator(DomainMetadata domainMetadata)
            => _domainMetadata = domainMetadata;

        public void ActivateMapping()
        {
            if (_persistentObjectMapper == null)
                return;

            var persistentObject = FilterPersistentTypeFormMetadata(_domainMetadata);

            foreach (var model in persistentObject)
            {
                var persistentObjInstance = ((IPersistentObject)Activator.CreateInstance(model));

                //Active mapping config.
                (persistentObjInstance as INeedParts<IPersistentObjectMapper>)?.SetParts(_persistentObjectMapper);
                persistentObjInstance.ConfigureMapping();
                (persistentObjInstance as IHasAccessor<IPersistentObjectMapConfig>)?.Instance?.Build();
            }
        }

        public void Dispose()
        {
            _domainMetadata = null;
            _persistentObjectMapper = null;
        }

        public void SetMapper(IPersistentObjectMapper mapper)
        {
            CheckValue.NotNull(mapper, nameof(mapper));

            _persistentObjectMapper = mapper;
        }

        private List<Type> FilterPersistentTypeFormMetadata(DomainMetadata domainMetadata)
        {
            return domainMetadata.DomainObject.AggregateRoots
                                 .Where(s => s.HasPersistentObject && s.PersistentObject != null)
                                 .Select(j => j.PersistentObject).ToList();
        }
    }
}
