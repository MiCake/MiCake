using MiCake.Cord.Storage.Internal;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.StorageInterpretor.Strategy
{
    internal class PropertyConfigStrategy : IConfigModelBuilderStrategy
    {
        public ModelBuilder Config(ModelBuilder modelBuilder, StoreEntityType storeEntity, Type efModelType)
        {
            var properties = storeEntity.GetProperties();

            if (!properties.Any())
                return modelBuilder;

            var entityBuilder = modelBuilder.Entity(efModelType);

            foreach (StoreProperty property in properties)
            {
                var propertyName = property.Name;
                var efcoreProperty = entityBuilder.Property(propertyName);

                //consider splitting into multiple Strategy classes?
                if (property.IsConcurrency.HasValue && property.IsConcurrency.Value)
                    efcoreProperty.IsConcurrencyToken(true);

                if (property.IsNullable.HasValue)
                    efcoreProperty.IsRequired(!property.IsNullable.Value);

                if (property.MaxLength.HasValue)
                    efcoreProperty.HasMaxLength(property.MaxLength.Value);

                // config default value.
                if (property.DefaultValue != null && property.DefaultValue.HasValue)
                {
                    var defaultValueConfig = property.DefaultValue.Value;

                    if (defaultValueConfig.ValueType == Cord.Storage.StorePropertyDefaultValueType.ClrValue)
                    {
                        efcoreProperty.HasDefaultValue(defaultValueConfig.DefaultValue);
                    }
                    else if (defaultValueConfig.ValueType == Cord.Storage.StorePropertyDefaultValueType.SqlValue)
                    {
                        efcoreProperty.HasDefaultValueSql(defaultValueConfig.DefaultValue as string);
                    }

                    if (defaultValueConfig.SetOpportunity == Cord.Storage.StorePropertyDefaultValueSetOpportunity.Add)
                    {
                        efcoreProperty.ValueGeneratedOnAdd();
                    }
                    else if (defaultValueConfig.SetOpportunity == Cord.Storage.StorePropertyDefaultValueSetOpportunity.Update)
                    {
                        efcoreProperty.ValueGeneratedOnUpdate();
                    }
                    else if (defaultValueConfig.SetOpportunity == Cord.Storage.StorePropertyDefaultValueSetOpportunity.AddAndUpdate)
                    {
                        efcoreProperty.ValueGeneratedOnAddOrUpdate();
                    }
                }
            }

            return modelBuilder;
        }
    }
}
