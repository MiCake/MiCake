using MiCake.Core;
using MiCake.Core.Util.Reflection;
using MiCake.DDD.CQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace MiCake
{
    public static class MiCakeCQSExtension
    {
        /// <summary>
        /// Add CQS for MiCake
        /// </summary>
        /// <param name="builder"><see cref="IMiCakeBuilder"/></param>
        /// <param name="CQSAssemblies">The assembly in which the CQS resides</param>
        /// <returns></returns>
        public static IMiCakeBuilder UseCQS(this IMiCakeBuilder builder, params Assembly[] CQSAssemblies)
        {
            builder.ConfigureApplication((app, services) =>
            {
                //Add CQS module.
                app.SlotModule(typeof(MiCakeCQSModule));

                ResgisterAllCQSHandler(services, CQSAssemblies);

                services.TryAddScoped<IQueryDispatcher, QueryDispatcher>();
                services.TryAddScoped<ICommandDispatcher, CommandDispatcher>();
            });

            return builder;
        }

        public static void ResgisterAllCQSHandler(IServiceCollection services, Assembly[] CQSAssemblies)
        {
            // reigster all query handlers

            var allQueryHandlers = CQSAssemblies.SelectMany(s => s.GetTypes()).Where(s => TypeHelper.IsConcrete(s) && TypeHelper.IsImplementedGenericInterface(s, typeof(IQueryHandler<,>)));
            foreach (var handleType in allQueryHandlers)
            {
                var allInterfaces = handleType.GetInterfaces().Where(s => s.IsGenericType && s.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

                foreach (var serviceType in allInterfaces)
                {
                    services.AddScoped(serviceType, handleType);
                }
            }

            // register all command handerls

            var allCommandHandlers = CQSAssemblies.SelectMany(s => s.GetTypes()).Where(s => TypeHelper.IsConcrete(s) && TypeHelper.IsImplementedGenericInterface(s, typeof(ICommandHandler<>)));
            foreach (var handleType in allCommandHandlers)
            {
                var allInterfaces = handleType.GetInterfaces().Where(s => s.IsGenericType && s.GetGenericTypeDefinition() == typeof(ICommandHandler<>));

                foreach (var serviceType in allInterfaces)
                {
                    services.AddScoped(serviceType, handleType);
                }
            }
        }
    }
}
