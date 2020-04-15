using MiCake.DDD.Extensions.Store.Configure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace MiCake.EntityFrameworkCore.Interprets.Strategies
{
    internal class PropertyConfigStrategy : IConfigModelBuilderStrategy
    {
        public ModelBuilder Config(ModelBuilder modelBuilder, StoreEntityType storeEntity, Type efModelType)
        {
            var properties = storeEntity.GetProperties();

            if (properties.Count() == 0)
                return modelBuilder;

            var entityBuilder = modelBuilder.Entity(efModelType);

            foreach (StoreProperty property in properties)
            {
                var propertyName = property.Name;

                //consider splitting into multiple Strategy classes?
                if (property.IsConcurrency.HasValue && property.IsConcurrency.Value)
                    entityBuilder.Property(propertyName).IsConcurrencyToken(true);

                if (property.DefaultValue != null)
                    entityBuilder.Property(propertyName).HasDefaultValue(property.DefaultValue);

                if (property.IsNullable.HasValue)
                    entityBuilder.Property(propertyName).IsRequired(!property.IsNullable.Value);

                if (property.MaxLength.HasValue)
                    entityBuilder.Property(propertyName).HasMaxLength(property.MaxLength.Value);
            }

            return modelBuilder;
        }
    }
}
