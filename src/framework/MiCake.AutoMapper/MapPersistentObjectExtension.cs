using AutoMapper;
using MiCake.DDD.Extensions.Store;
using Microsoft.Extensions.DependencyInjection;

namespace MiCake.AutoMapper
{
    internal static class MapPersistentObjectExtension
    {
        public static void ConfigPersistentObject(this IMapperConfigurationExpression configExpression, IServiceCollection services)
        {
            //Give temp scope to active IPersistentObjectActivator.
            using (var tempScope = services.BuildServiceProvider().CreateScope())
            {
                var provider = tempScope.ServiceProvider;

                var mapper = new AutoMapperPersistentObjectMapper(null);
                mapper.SetAutoMapperConfigExpression(configExpression);

                var poActivator = provider.GetService<IPersistentObjectActivator>();
                poActivator.SetMapper(mapper);
                poActivator.ActivateMapping();
            }
        }
    }
}
