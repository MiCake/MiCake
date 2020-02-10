using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Domain.Freedom;
using MiCake.Core.Util.Reflection;

namespace MiCake.DDD.Extensions.Register
{
    public abstract class DefaultRepositoryRegister : IRepositoryRegister
    {
        /// <summary>
        /// need register default repository
        /// </summary>
        protected virtual bool IsRegisterDefaultRepository => true;

        /// <summary>
        /// need register default free repository
        /// </summary>
        protected virtual bool IsRegisterFreeRepository => true;

        /// <summary>
        /// Place the assembly of the domain object
        /// If it is not indicated that the assembly system will traverse all user module lookups
        /// </summary>
        protected virtual Assembly[] DomainObjectAssembly => null;

        public virtual void Register(IMiCakeModuleCollection miCakeModules, IServiceCollection services)
        {
            var hasDomainObjectModules = DomainObjectAssembly == null ?
                                                FindDomainObjectAsm(miCakeModules) :
                                                DomainObjectAssembly;

            if (hasDomainObjectModules.Length == 0)
                return;

            if (IsRegisterDefaultRepository)
                RegisterDefaultRepository(hasDomainObjectModules, services);

            if (IsRegisterFreeRepository)
                RegisterFreeRepository(hasDomainObjectModules, services);
        }

        protected virtual Assembly[] FindDomainObjectAsm(IMiCakeModuleCollection miCakeModules)
        {
            var customerModules = miCakeModules.Where(modules => !modules.ModuleInstance.IsFrameworkLevel);
            return customerModules.Where(module =>
                    module.Assembly.GetTypes().AsEnumerable().Any(inModuleType =>
                         typeof(IEntity).IsAssignableFrom(inModuleType))).Select(
                                module => module.Assembly).ToArray();

        }

        #region aggragateRoot Repository
        protected virtual void RegisterDefaultRepository(Assembly[] hasDomainObjectAsm, IServiceCollection services)
        {
            var aggregateRoots = new List<Type>();
            foreach (var assembly in hasDomainObjectAsm)
            {
                aggregateRoots.AddRange(assembly.GetTypes().AsEnumerable()
                    .Where(type => TypeHelper.IsConcrete(type))
                    .Where(type => EntityHelper.IsAggregateRoot(type)));
            }

            foreach (var aggregateRoot in aggregateRoots)
            {
                var impType = GetAggregateRepositoryImplementationType(aggregateRoot);
                if (impType != null)
                    RegisterAggregateRepositoryToServices(aggregateRoot, impType, services);
            }
        }

        protected abstract Type GetAggregateRepositoryImplementationType(Type entityTypet);

        protected virtual void RegisterAggregateRepositoryToServices(
            Type entityType, Type ImpType,
            IServiceCollection services)
        {
            var primaryKeyType = EntityHelper.FindPrimaryKeyType(entityType);
            if (primaryKeyType != null && EntityHelper.IsAggregateRoot(entityType))
            {
                var readOnlyRepository = typeof(IReadOnlyRepository<,>).MakeGenericType(entityType, primaryKeyType);
                services.AddTransient(readOnlyRepository, ImpType);

                var repository = typeof(IRepository<,>).MakeGenericType(entityType, primaryKeyType);
                services.AddTransient(repository, ImpType);
            }
        }

        #endregion

        #region Free Repository

        protected virtual void RegisterFreeRepository(Assembly[] hasDomainObjectAsm, IServiceCollection services)
        {
            var entitys = new List<Type>();
            foreach (var assembly in hasDomainObjectAsm)
            {
                entitys.AddRange(assembly.GetTypes().AsEnumerable()
                    .Where(type => TypeHelper.IsConcrete(type))
                    .Where(type => EntityHelper.IsEntity(type)));
            }

            foreach (var entity in entitys)
            {
                var impType = GetFreeRepositoryImplementationType(entity);
                if (impType != null)
                    RegisterFreeRepositoryToServices(entity, impType, services);
            }
        }

        protected abstract Type GetFreeRepositoryImplementationType(Type entityType);

        protected virtual void RegisterFreeRepositoryToServices(
            Type entityType, Type ImpType,
            IServiceCollection services)
        {
            var primaryKeyType = EntityHelper.FindPrimaryKeyType(entityType);
            if (primaryKeyType != null && EntityHelper.IsEntity(entityType))
            {
                var readOnlyFreeRepository = typeof(IReadOnlyFreeRepository<,>).MakeGenericType(entityType, primaryKeyType);
                services.AddTransient(readOnlyFreeRepository, ImpType);

                var freeRepository = typeof(IFreeRepository<,>).MakeGenericType(entityType, primaryKeyType);
                services.AddTransient(freeRepository, ImpType);
            }
        }

        #endregion
    }
}
