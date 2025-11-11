using MiCake.Core.Modularity;
using MiCake.Util;
using MiCake.Util.Reflection;
using MiCake.DDD.Domain.Helper;
using System;
using System.Linq;

namespace MiCake.DDD.Infrastructure.Metadata
{
    internal class DefaultDomainObjectModelProvider : IDomainObjectModelProvider
    {
        private readonly IMiCakeModuleCollection _exceptedModules;

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
            foreach (var findType in allTypes)
            {
                if (!DomainTypeHelper.IsDomainObject(findType))
                    continue;

                var entityDes = GetEntityDescriptor(findType);
                var aggregateRootDes = GetAggregateRootDescriptor(findType);
                var valueObjectDes = GetValueObjectDescriptor(findType);

                if (entityDes != null) context.Result.Entities.Add(entityDes);
                if (aggregateRootDes != null) context.Result.AggregateRoots.Add(aggregateRootDes);
                if (valueObjectDes != null) context.Result.ValueObjects.Add(valueObjectDes);
            }
        }

        public void OnProvidersExecuted(DomainObjectModelContext context)
        {
        }

        private static EntityDescriptor GetEntityDescriptor(Type type)
        {
            if (!DomainTypeHelper.IsEntity(type))
                return null;

            return new EntityDescriptor(type);
        }

        private static AggregateRootDescriptor GetAggregateRootDescriptor(Type type)
        {
            if (!DomainTypeHelper.IsAggregateRoot(type))
                return null;

            var result = new AggregateRootDescriptor(type);
            return result;
        }

        private static VauleObjectDescriptor GetValueObjectDescriptor(Type type)
        {
            if (!DomainTypeHelper.IsValueObject(type))
                return null;

            return new VauleObjectDescriptor(type);
        }
    }
}
