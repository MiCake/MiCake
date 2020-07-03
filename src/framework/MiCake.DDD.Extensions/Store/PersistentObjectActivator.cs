using MiCake.Core.Data;
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
    internal class PersistentObjectActivator : IPersistentObjectActivator
    {
        private DomainMetadata _domainMetadata;
        private IPersistentObjectMapper _persistentObjectMapper;

        public PersistentObjectActivator(DomainMetadata domainMetadata, IPersistentObjectMapper persistentObjectMapper)
        {
            _domainMetadata = domainMetadata;
            _persistentObjectMapper = persistentObjectMapper;
        }

        public void ActivateMapping()
        {
            var persistentObject = FilterPersistentTypeFormMetadata(_domainMetadata);

            foreach (var model in persistentObject)
            {
                var persistentObjInstance = ((IPersistentObject)Activator.CreateInstance(model));

                //Active mapping config.
                (persistentObjInstance as INeedParts<IPersistentObjectMapper>)?.SetParts(_persistentObjectMapper);
                persistentObjInstance.ConfigureMapping();
            }
        }

        private List<Type> FilterPersistentTypeFormMetadata(DomainMetadata domainMetadata)
        {
            return domainMetadata.DomainObject.AggregateRoots
                                 .Where(s => s.HasPersistentObject && s.PersistentObject != null)
                                 .Select(j => j.PersistentObject).ToList();
        }
    }
}
