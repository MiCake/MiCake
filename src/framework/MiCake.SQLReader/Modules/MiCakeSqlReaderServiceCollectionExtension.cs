﻿using MiCake.Core.Data;
using MiCake.SqlReader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;

namespace MiCake
{
    public static class MiCakeSqlReaderServiceCollectionExtension
    {
        public static IServiceCollection AddSqlReader(this IServiceCollection services, Action<MiCakeSqlReaderOptions> optionsAction = null)
        {
            MiCakeSqlReaderOptions options = new();
            optionsAction?.Invoke(options);

            var providers = (options as IHasAccessor<IEnumerable<ISqlDataProvider>>).Instance;

            var manager = new SqlDataManager();
            foreach (var provider in providers)
            {
                manager.AddSqlDataProvider(provider);
            }

            manager.PopulateSql();

            services.TryAddSingleton<ISqlDataManager>(manager);
            services.TryAddSingleton<ISqlReader, DefaultSqlReader>();

            return services;
        }
    }
}
