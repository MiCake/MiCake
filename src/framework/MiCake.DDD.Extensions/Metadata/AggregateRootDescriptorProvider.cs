using MiCake.Core.Modularity;
using MiCake.Core.Util.Reflection;
using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Extensions.Store;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Extensions.Metadata
{
    public class AggregateRootDescriptorProvider : IObjectDescriptorProvider
    {
        private List<Type> storageModels = new List<Type>();

        public AggregateRootDescriptorProvider(IMiCakeModuleCollection miCakeModules)
        {
            FindStorageModels(miCakeModules);
        }

        private void FindStorageModels(IMiCakeModuleCollection miCakeModules)
        {
            var ranges = miCakeModules.GetAssemblies(false).SelectMany(s =>
               s.GetTypes().Where(type => TypeHelper.IsConcrete(type) && typeof(IStorageModel).IsAssignableFrom(type)));

            if (ranges.Count() > 0)
                storageModels.AddRange(ranges);
        }

        public IObjectDescriptor GetDescriptor(Type type)
        {
            if (!EntityHelper.IsAggregateRoot(type))
                return null;

            var result = new AggregateRootDescriptor(type);

            //get storage Model
            var findStorageModel = storageModels.FirstOrDefault(s =>
                                        TypeHelper.GetGenericArguments(s, typeof(IStorageModel<>)).FirstOrDefault() == type);

            if (findStorageModel != null)
                result.SetStorageModel(findStorageModel);

            return result;
        }

        public void Dispose()
        {
            storageModels.Clear();
            storageModels = null;
        }
    }
}
