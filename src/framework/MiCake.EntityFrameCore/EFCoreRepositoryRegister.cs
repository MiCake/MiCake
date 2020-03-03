using JetBrains.Annotations;
using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Extensions.Register;
using MiCake.EntityFrameworkCore.Repository;
using MiCake.EntityFrameworkCore.Repository.Freedom;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore
{
    internal class EFCoreRepositoryRegister : DefaultRepositoryRegister
    {
        protected override bool IsRegisterDefaultRepository => _options.RegisterDefaultRepository;
        protected override bool IsRegisterFreeRepository => _options.RegisterFreeRepository;

        private MiCakeEFCoreOptions _options;

        public EFCoreRepositoryRegister(IServiceCollection services, [NotNull]MiCakeEFCoreOptions options) : base(services)
        {
            _options = options;
        }

        protected override Type GetAggregateRepositoryImplementationType(AggregateRootDescriptor descriptor)
        {
            Type impType;
            if (descriptor.HasStorageModel && descriptor.StorageModel != null)
            {
                impType = typeof(EFStorageModelRepository<,,,>)
                            .MakeGenericType(_options.DbContextType, descriptor.Type, descriptor.StorageModel, descriptor.PrimaryKey);
            }
            else
            {
                impType = typeof(EFRepository<,,>)
                            .MakeGenericType(_options.DbContextType, descriptor.Type, descriptor.PrimaryKey);
            }

            return impType;
        }

        protected override Type GetFreeRepositoryImplementationType(EntityDescriptor descriptor)
        {
            return typeof(EFFreeRepository<,,>)
                            .MakeGenericType(_options.DbContextType, descriptor.Type, descriptor.PrimaryKey);
        }
    }
}
