using MiCake.Core.Modularity;
using MiCake.Core.Util.Reflection;
using MiCake.DDD.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.DDD.Extensions.Metadata
{
    public class DomainMetadataCreator : IDisposable
    {
        private List<IObjectDescriptorProvider> providers = new List<IObjectDescriptorProvider>();
        private IMiCakeModuleCollection _miCakeModules;
        private bool isDispose = false;

        public DomainMetadataCreator(IMiCakeModuleCollection miCakeModules)
        {
            _miCakeModules = miCakeModules;
        }

        public void AddDescriptorProvider(IObjectDescriptorProvider provider)
        {
            providers.Add(provider);
        }

        public virtual IDomainMetadata Create()
        {
            var allObjectDescriptor = GetObjectDescriptors();

            var domainMetadata = new DomainMetadata();

            domainMetadata.Entities = allObjectDescriptor.Where(s => s is EntityDescriptor).Cast<EntityDescriptor>().ToList();
            domainMetadata.AggregateRoots = allObjectDescriptor.Where(s => s is AggregateRootDescriptor).Cast<AggregateRootDescriptor>().ToList();
            domainMetadata.DomainLayerAssembly = GetDomianLayer();

            return domainMetadata;
        }

        private List<IObjectDescriptor> GetObjectDescriptors()
        {
            var descriptors = new List<IObjectDescriptor>();

            var asm = _miCakeModules.GetAssemblies(false);
            var types = asm.SelectMany(s => s.GetTypes().Where(type => TypeHelper.IsConcrete(type)));

            foreach (var findType in types)
            {
                foreach (var provider in providers)
                {
                    var descriptor = provider.GetDescriptor(findType);

                    if (descriptor != null)
                        descriptors.Add(descriptor);
                }
            }

            return descriptors;
        }

        private Assembly[] GetDomianLayer()
        {
            return _miCakeModules.GetAssemblies(false).Where(asm =>
                            asm.GetTypes().AsEnumerable().Any(inModuleType =>
                                            typeof(IEntity).IsAssignableFrom(inModuleType))).ToArray();
        }

        public void Dispose()
        {
            if (!isDispose)
            {
                isDispose = true;
                foreach (var provider in providers)
                {
                    provider.Dispose();
                }

                providers.Clear();
                providers = null;
                _miCakeModules = null;
            }
        }
    }
}
