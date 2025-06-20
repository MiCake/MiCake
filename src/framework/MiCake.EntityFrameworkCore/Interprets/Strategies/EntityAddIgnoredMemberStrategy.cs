﻿using MiCake.DDD.Extensions.Store.Configure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace MiCake.EntityFrameworkCore.Interprets.Strategies
{
    internal class EntityAddIgnoredMemberStrategy : IConfigModelBuilderStrategy
    {
        public ModelBuilder Config(ModelBuilder modelBuilder, StoreEntityType storeEntity, Type efModelType)
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
