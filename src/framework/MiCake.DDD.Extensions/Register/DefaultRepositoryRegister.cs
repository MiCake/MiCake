using JetBrains.Annotations;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Freedom;
using MiCake.DDD.Domain.Helper;
using MiCake.DDD.Extensions.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

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
        /// <see cref="IDomainMetadata"/>
        /// </summary>
        protected virtual IDomainMetadata DomainMetadata { get; private set; }

        public DefaultRepositoryRegister([NotNull]IServiceCollection services)
        {
            var domainMetadata = services.BuildServiceProvider().GetService<IDomainMetadata>();
            if (domainMetadata == null)
                throw new NullReferenceException($"Please make sure {nameof(IDomainMetadata)} has created.");

            DomainMetadata = domainMetadata;
        }

        public virtual void Register(IMiCakeModuleCollection miCakeModules, IServiceCollection services)
        {
            if (DomainMetadata.Entities.Count == 0)
                return;

            if (IsRegisterDefaultRepository)
                RegisterDefaultRepository(DomainMetadata.AggregateRoots, services);

            if (IsRegisterFreeRepository)
                RegisterFreeRepository(DomainMetadata.Entities, services);
        }

        #region aggragateRoot Repository
        protected virtual void RegisterDefaultRepository(List<AggregateRootDescriptor> aggregateRootDescriptors, IServiceCollection services)
        {
            foreach (var descriptor in aggregateRootDescriptors)
            {
                var impType = GetAggregateRepositoryImplementationType(descriptor);
                if (impType != null)
                    RegisterAggregateRepositoryToServices(descriptor.Type, impType, services);
            }
        }

        protected abstract Type GetAggregateRepositoryImplementationType(AggregateRootDescriptor descriptor);

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

        protected virtual void RegisterFreeRepository(List<EntityDescriptor> entityDescriptors, IServiceCollection services)
        {
            foreach (var descriptor in entityDescriptors)
            {
                var impType = GetFreeRepositoryImplementationType(descriptor);
                if (impType != null)
                    RegisterFreeRepositoryToServices(descriptor.Type, impType, services);
            }
        }

        protected abstract Type GetFreeRepositoryImplementationType(EntityDescriptor descriptor);

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
