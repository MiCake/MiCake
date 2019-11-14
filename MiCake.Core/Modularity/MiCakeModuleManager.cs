using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Util.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiCake.Core.Modularity
{
    public class MiCakeModuleManager : IMiCakeModuleManager
    {
        private bool isPopulate;

        public IMiCakeModuleCollection miCakeModules { get; } = new MiCakeModuleCollection();

        public void PopulateDefaultModule(Type startUp)
        {
            if (isPopulate)
                throw new InvalidOperationException("PopulateDefaultModule can only be called once.");

            isPopulate = true;

            foreach (var module in ResolvingMiCakeModule(startUp))
            {
                miCakeModules.Add(module);
            }
        }

        internal void ActivateServices(IServiceCollection services, Action<IMiCakeModuleCollection> otherPartActivateAction)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (!isPopulate)
                throw new InvalidOperationException("MiCake Modules is not activate. Please run PopulateDefaultModule(startUp) first.");

            var logger = services.BuildServiceProvider().GetRequiredService<ILogger<MiCakeModuleManager>>();

            logger.LogInformation("MiCake:ActivateServices...");

            var context = new ModuleConfigServiceContext(services);

            //PreConfigServices
            foreach (var miCakeModule in miCakeModules)
            {
                logger.LogInformation($"MiCake LiftTime-PreConfigServices:{ miCakeModule.Type.Name }");
                miCakeModule.ModuleInstance.PreConfigServices(context);
            }
            //ConfigServiices
            foreach (var miCakeModule in miCakeModules)
            {
                logger.LogInformation($"MiCake ConfigServiices:{ miCakeModule.Type.Name }");
                miCakeModule.ModuleInstance.ConfigServices(context);
            }

            //Activate Other Part Services 
            otherPartActivateAction?.Invoke(miCakeModules);

            //PostConfigServices
            foreach (var miCakeModule in miCakeModules)
            {
                logger.LogInformation($"MiCake LiftTime-OnStart:{ miCakeModule.Type.Name }");
                miCakeModule.ModuleInstance.PostConfigServices(context);
            }

            logger.LogInformation("MiCake:ActivateServices Completed");
        }

        private IEnumerable<MiCakeModuleDescriptor> ResolvingMiCakeModule(Type startUp)
        {
            List<MiCakeModuleDescriptor> miCakeModuleDescriptors = new List<MiCakeModuleDescriptor>();

            var moduleTypes = MiCakeModuleHelper.FindAllModuleTypes(startUp);
            foreach (var moduleTye in moduleTypes)
            {
                MiCakeModule instance = (MiCakeModule)Activator.CreateInstance(moduleTye);
                miCakeModuleDescriptors.Add(new MiCakeModuleDescriptor(moduleTye, instance));
            }

            miCakeModuleDescriptors.SortByDependencies(s => s.Dependencies);

            //获取Module的依赖关系
            foreach (var miCakeModuleDescriptor in miCakeModuleDescriptors)
            {
                var depencies = GetMiCakeModuleDescriptorDepencyies(miCakeModuleDescriptors, miCakeModuleDescriptor);

                foreach (var depency in depencies)
                {
                    miCakeModuleDescriptor.AddDependency(depency);
                }
            }

            return miCakeModuleDescriptors;
        }

        private List<MiCakeModuleDescriptor> GetMiCakeModuleDescriptorDepencyies(List<MiCakeModuleDescriptor> modules, MiCakeModuleDescriptor moduleDescriptor)
        {
            List<MiCakeModuleDescriptor> descriptors = new List<MiCakeModuleDescriptor>();

            var depencyTypes = MiCakeModuleHelper.FindDependedModuleTypes(moduleDescriptor.Type);

            foreach (var depencyType in depencyTypes)
            {
                var existDescriptor = modules.FirstOrDefault(s => s.Type == depencyType);
                if (existDescriptor != null) moduleDescriptor.AddDependency(existDescriptor);
            }

            return descriptors;
        }
    }
}
