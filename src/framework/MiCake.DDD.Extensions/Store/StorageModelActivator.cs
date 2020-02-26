using MiCake.DDD.Extensions.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Extensions.Store
{
    /// <summary>
    /// This class is used to automatically call the mapping configuration method of storage model
    /// </summary>
    internal class StorageModelActivator
    {
        private IDomainMetadata _domainMetadata;

        public StorageModelActivator(IDomainMetadata domainMetadata)
        {
            _domainMetadata = domainMetadata;
        }

        public virtual void LoadConfigMapping()
        {
            var storageModels = FilterStorageModelFormMetadata(_domainMetadata);

            foreach (var model in storageModels)
            {
                ((IStorageModel)Activator.CreateInstance(model)).ConfigureMapping();
            }
        }

        private List<Type> FilterStorageModelFormMetadata(IDomainMetadata domainMetadata)
        {
            return domainMetadata.AggregateRoots.Where(s => s.HasStorageModel && s.StorageModel != null)
                                                .Select(j => j.StorageModel).ToList();
        }
    }
}
