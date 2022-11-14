﻿using MiCake.Cord.Storage;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.StorageInterpretor.Strategy
{
    internal class EntityAddIgnoredMemberStrategy : IConfigModelBuilderStrategy
    {
        public ModelBuilder Config(ModelBuilder modelBuilder, IConventionStoreEntity storeEntity, Type efModelType)
        {
            var ignoredMembers = storeEntity.GetIgnoredMembers();

            if (!ignoredMembers.Any())
                return modelBuilder;

            var entityBuilder = modelBuilder.Entity(efModelType);
            foreach (var member in ignoredMembers)
            {
                entityBuilder.Ignore(member);
            }

            return modelBuilder;
        }
    }
}
