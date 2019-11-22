using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Util.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MiCake.Core.Modularity
{
    public class MiCakeModuleManager : IMiCakeModuleManager
    {
        public IMiCakeModuleCollection MiCakeModules { get; } = new MiCakeModuleCollection();
        public IMiCakeModuleCollection FeatureModules { get; } = new MiCakeModuleCollection();

        private bool _isPopulate;
        private IList<IFeatureModule> _featureModules = new List<IFeatureModule>();

        internal void PopulateModules(Type startUp)
        {
            if (_isPopulate)
                throw new InvalidOperationException("PopulateDefaultModule can only be called once.");

            _isPopulate = true;

            //normal module
            foreach (var module in ResolvingMiCakeModules(startUp))
            {
                MiCakeModules.AddIfNotContains(module);
            }

            //feature module
            foreach (var featureModule in _featureModules)
            {
                if (!featureModule.AutoRegisted) continue;

                var moduleDescript = new MiCakeModuleDescriptor(featureModule.GetType(), (MiCakeModule)featureModule);
                FeatureModules.AddIfNotContains(moduleDescript);
            }
            SortModulesDepencyies(FeatureModules.ToList());
        }

        public MiCakeModuleDescriptor GetMiCakeModule(Type moduleType)
        {
            return MiCakeModules.FirstOrDefault(s => s.Type == moduleType);
        }

        public void AddFeatureModule(IFeatureModule featureModule)
        {
            MiCakeModuleHelper.CheckModule(featureModule.GetType());

            _featureModules.AddIfNotContains(featureModule);
        }

        internal void ConfigServices(IServiceCollection services, Action<IMiCakeModuleCollection> otherPartActivateAction)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (!_isPopulate)
                throw new InvalidOperationException("MiCake Modules is not activate. Please run PopulateModules(startUp) first.");

            var logger = services.BuildServiceProvider().GetRequiredService<ILogger<MiCakeModuleManager>>();

            logger.LogInformation("MiCake:ActivateServices...");

            var context = new ModuleConfigServiceContext(services);
            var allModules = MiCakeModuleHelper.CombineNoralAndFeatureModules(MiCakeModules, FeatureModules);

            //PreConfigServices
            foreach (var miCakeModule in allModules)
            {
                logger.LogInformation($"MiCake LiftTime-PreConfigServices:{ GetModuleInfoString(miCakeModule) }");
                miCakeModule.ModuleInstance.PreConfigServices(context);
            }
            //ConfigServices
            foreach (var miCakeModule in allModules)
            {
                logger.LogInformation($"MiCake ConfigServiices:{ GetModuleInfoString(miCakeModule) }");
                miCakeModule.ModuleInstance.ConfigServices(context);
            }
            //PostConfigServices
            foreach (var miCakeModule in allModules)
            {
                logger.LogInformation($"MiCake LiftTime-PostConfigServices:{ GetModuleInfoString(miCakeModule) }");
                miCakeModule.ModuleInstance.PostConfigServices(context);
            }

            //Activate Other Part Services 
            otherPartActivateAction?.Invoke(allModules);

            logger.LogInformation("MiCake:ActivateServices Completed.....");
        }

        private IEnumerable<MiCakeModuleDescriptor> ResolvingMiCakeModules(Type startUp)
        {
            List<MiCakeModuleDescriptor> miCakeModuleDescriptors = new List<MiCakeModuleDescriptor>();

            var moduleTypes = MiCakeModuleHelper.FindAllModuleTypes(startUp);
            foreach (var moduleTye in moduleTypes)
            {
                MiCakeModule instance = (MiCakeModule)Activator.CreateInstance(moduleTye);
                miCakeModuleDescriptors.Add(new MiCakeModuleDescriptor(moduleTye, instance));
            }

            return SortModulesDepencyies(miCakeModuleDescriptors);
        }

        //Get module dependencies
        private List<MiCakeModuleDescriptor> SortModulesDepencyies(List<MiCakeModuleDescriptor> miCakeModuleDescriptors)
        {
            foreach (var miCakeModuleDescriptor in miCakeModuleDescriptors)
            {
                var depencies = GetMiCakeModuleDescriptorDepencyies(miCakeModuleDescriptors, miCakeModuleDescriptor);

                foreach (var depency in depencies)
                {
                    miCakeModuleDescriptor.AddDependency(depency);
                }
            }
            miCakeModuleDescriptors.SortByDependencies(s => s.Dependencies);

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

        private string GetModuleInfoString(MiCakeModuleDescriptor moduleDesciptor)
        {
            var featerTag = (moduleDesciptor.ModuleInstance is IFeatureModule) ? "[Feature] - " : string.Empty;
            return featerTag + moduleDesciptor.Type.Name;
        }
    }
}
