using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore.Repository.Freedom;
using System;
using System.Reflection;
using JetBrains.Annotations;
using MiCake.DDD.Extensions.Register;

namespace MiCake.EntityFrameworkCore
{
    internal class EFCoreRepositoryRegister : DefaultRepositoryRegister
    {
        protected override bool IsRegisterDefaultRepository => _options.RegisterDefaultRepository;
        protected override bool IsRegisterFreeRepository => _options.RegisterFreeRepository;
        protected override Assembly[] DomainObjectAssembly => _options.DomainObjectAssembly;

        private MiCakeEFCoreOptions _options;

        public EFCoreRepositoryRegister([NotNull]MiCakeEFCoreOptions options)
        {
            _options = options;
        }

        protected override Type GetAggregateRepositoryImplementationType(Type entityType)
        {
            var primaryKeyType = EntityHelper.FindPrimaryKeyType(entityType);
            if (primaryKeyType == null || (entityType is IAggregateRoot))
                return null;

            Type impType;
            if (EntityHelper.IsEntityHasSnapshot(entityType))
            {
                var snapshotType = EntityHelper.FindEntitySnapshotType(entityType);
                impType = typeof(EFSnapshotRepository<,,,>)
                            .MakeGenericType(_options.DbContextType, entityType, snapshotType, primaryKeyType);
            }
            else
            {
                impType = typeof(EFRepository<,,>)
                            .MakeGenericType(_options.DbContextType, entityType, primaryKeyType);
            }

            return impType;
        }

        protected override Type GetFreeRepositoryImplementationType(Type entityType)
        {
            var primaryKeyType = EntityHelper.FindPrimaryKeyType(entityType);
            if (primaryKeyType == null || (entityType is IEntity))
                return null;

            return typeof(EFFreeRepository<,,>)
                            .MakeGenericType(_options.DbContextType, entityType, primaryKeyType);
        }
    }
}
