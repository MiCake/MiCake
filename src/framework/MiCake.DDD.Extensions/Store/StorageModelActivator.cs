using MiCake.DDD.Extensions.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// This class is used to automatically call the mapping configuration method of storage model
    /// </summary>
    internal class StorageModelActivator : IStorageModelActivator
    {
        private DomainMetadata _domainMetadata;

        public StorageModelActivator(DomainMetadata domainMetadata)
        {
            _domainMetadata = domainMetadata;
        }

        public void ActivateMapping()
        {
            var storageModels = FilterStorageModelFormMetadata(_domainMetadata);

            foreach (var model in storageModels)
            {
                ((IStorageModel)Activator.CreateInstance(model)).ConfigureMapping();
            }
        }

        private List<Type> FilterStorageModelFormMetadata(DomainMetadata domainMetadata)
        {
            return domainMetadata.DomainObject.AggregateRoots.Where(s => s.HasStorageModel && s.StorageModel != null)
                                                .Select(j => j.StorageModel).ToList();
        }
    }
}
