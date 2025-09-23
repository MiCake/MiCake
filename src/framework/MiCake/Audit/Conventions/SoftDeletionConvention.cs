using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Extensions.Store;
using System;
using System.Linq.Expressions;

namespace MiCake.Audit.Conventions
{
    /// <summary>
    /// Convention for soft deletion behavior
    /// </summary>
    public class SoftDeletionConvention : IEntityConvention
    {
        public int Priority => 100;
        
        public bool CanApply(Type entityType)
        {
            return typeof(ISoftDeletion).IsAssignableFrom(entityType);
        }
        
        public void Configure(Type entityType, EntityConventionContext context)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
                
            context.EnableSoftDeletion = true;
            context.EnableDirectDeletion = false;
            
            // Create query filter: entity => !entity.IsDeleted
            var parameter = Expression.Parameter(entityType, "entity");
            var property = Expression.Property(parameter, nameof(ISoftDeletion.IsDeleted));
            var notDeleted = Expression.Not(property);
            context.QueryFilter = Expression.Lambda(notDeleted, parameter);
        }
    }
}