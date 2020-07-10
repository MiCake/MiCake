using MiCake.Core.Modularity;
using MiCake.Core.Util;
using MiCake.Core.Util.Reflection;
using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Extensions.Store;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.DDD.Extensions.Metadata
{
    internal class DefaultDomainObjectModelProvider : IDomainObjectModelProvider
    {
        private IMiCakeModuleCollection _exceptedModules;

        public DefaultDomainObjectModelProvider(
            IMiCakeModuleContext moduleContext)
        {
            CheckValue.NotNull(moduleContext, nameof(moduleContext));

            _exceptedModules = moduleContext.MiCakeModules.Where(s => !s.Instance.IsFrameworkLevel)
                                            .ToMiCakeModuleCollection();
        }

        public int Order => -1000;

        public void OnProvidersExecuting(DomainObjectModelContext context)
        {
            var allTypes = context.DomainLayerAssembly.SelectMany(s => s.GetTypes().Where(type => TypeHelper.IsConcrete(type)));

            var persistentTypes = FindPersistentTypes();

            foreach (var findType in allTypes)
            {
                if (!DomainTypeHelper.IsDomainObject(findType))
                    continue;

                var entityDes = GetEntityDescriptor(findType);
                var aggregateRootDes = GetAggregateRootDescriptor(findType, persistentTypes);
                var valueObjectDes = GetValueObjectDescriptor(findType);

                if (entityDes != null) context.Result.Entities.Add(entityDes);
                if (aggregateRootDes != null) context.Result.AggregateRoots.Add(aggregateRootDes);
                if (valueObjectDes != null) context.Result.VauleObjects.Add(valueObjectDes);
            }
        }

        public void OnProvidersExecuted(DomainObjectModelContext context)
        {
        }

        private List<Type> FindPersistentTypes()
        {
            return _exceptedModules.GetAssemblies(false).SelectMany(s =>
                                s.GetTypes().Where(type =>
                                     TypeHelper.IsConcrete(type) && typeof(IPersistentObject).IsAssignableFrom(type))).ToList();
        }

        private EntityDescriptor GetEntityDescriptor(Type type)
        {
            if (!DomainTypeHelper.IsEntity(type))
                return null;

            return new EntityDescriptor(type);
        }

        private AggregateRootDescriptor GetAggregateRootDescriptor(Type type, List<Type> persistentTypes)
        {
            if (!DomainTypeHelper.IsAggregateRoot(type))
                return null;

            var result = new AggregateRootDescriptor(type);

            //get persistent object.
            var currentPersistentType = persistentTypes.FirstOrDefault(s =>
                                        TypeHelper.GetGenericArguments(s, typeof(IPersistentObject<,>))?[1] == type);

            if (currentPersistentType != null)
                result.SetPersistentObject(currentPersistentType);

            return result;
        }

        private VauleObjectDescriptor GetValueObjectDescriptor(Type type)
        {
            if (!DomainTypeHelper.IsValueObject(type))
                return null;

            return new VauleObjectDescriptor(type);
        }
    }
}
