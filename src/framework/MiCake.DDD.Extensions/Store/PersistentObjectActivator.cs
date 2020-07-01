using MiCake.DDD.Extensions.Metadata;
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

        public PersistentObjectActivator(DomainMetadata domainMetadata)
        {
            _domainMetadata = domainMetadata;
        }

        public void ActivateMapping()
        {
            var persistentObject = FilterPersistentTypeFormMetadata(_domainMetadata);

            foreach (var model in persistentObject)
            {
                ((IPersistentObject)Activator.CreateInstance(model)).ConfigureMapping();
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
