using MiCake.Core.DependencyInjection;
using MiCake.DDD.Extensions.Store.Configure;
using MiCake.EntityFrameworkCore.Internal;
using MiCake.EntityFrameworkCore.Interprets;
using Microsoft.EntityFrameworkCore;
using System;

namespace MiCake.EntityFrameworkCore
{
    public static class EFCoreModelBuilderExtension
    {
        /// <summary>
        /// Add MiCake manage model for EFCore.
        /// If you don't inherit <see cref="MiCakeDbContext"/>, you can use this extension method in your DbContent OnModelCreating().
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ModelBuilder AddMiCakeModel(this ModelBuilder builder)
        {
            //EFCore will cached model info.This method only called once
            var storeModelExpression = new EFModelExpressionProvider().GetExpression();
            storeModelExpression.Interpret(StoreConfig.Instance.GetStoreModel(), builder);

            return builder;
        }

        /// <summary>
        /// Add MiCake configure for EFCore.(include repository lifetime etc.)
        /// If you don't inherit <see cref="MiCakeDbContext"/>, you can use this extension method in your DbContent OnConfiguring().
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static DbContextOptionsBuilder AddMiCakeConfigure(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
        {
            if (serviceProvider != null)
                optionsBuilder.AddInterceptors(new MiCakeEFCoreInterceptor(serviceProvider));
            return optionsBuilder;
        }
    }
}
