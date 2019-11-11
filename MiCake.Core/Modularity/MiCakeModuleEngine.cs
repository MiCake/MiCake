using MiCake.Core.Abstractions.Modularity;
using MiCake.Core.Extensions;
using MiCake.Core.Util.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.Modularity
{
    public abstract class MiCakeModuleEngine : IMiCakeModuleEngine
    {
        private IServiceCollection _services;
        private ILogger<MiCakeModuleEngine> _logger;

        private readonly IMiCakeModuleCollection _miCakeModules = new MiCakeModuleCollection();
        public IMiCakeModuleCollection MiCakeModules { get => _miCakeModules; }

        private Action<IMiCakeModuleCollection> _configureModule;

        public MiCakeModuleEngine(IServiceCollection services, ILogger<MiCakeModuleEngine> logger)
        {
            _services = services;
            _logger = logger;
        }

        public virtual IEnumerable<MiCakeModuleDescriptor> LoadMiCakeModules(Type startUpModule)
        {
            var miCakeDescriptors = ResolvingMiCakeModule(startUpModule);

            foreach (var item in miCakeDescriptors)
            {
                _miCakeModules.Add(item);
            }

            return miCakeDescriptors;
        }

        public virtual void InitializeModules()
        {

            var moduleContext = new ModuleContext(_services, _miCakeModules);

            _logger.LogInformation("MiCake:InitializeModules...");

            //PreStart
            foreach (var miCakeModule in _miCakeModules)
            {
                _logger.LogInformation($"MiCake LiftTime-PreStart:{ miCakeModule.Type.Name }");

                var miCakeLiftTime = (MiCakeModule)miCakeModule.ModuleInstance;
                miCakeLiftTime.PreStart(moduleContext);
            }

            //Start
            foreach (var miCakeModule in _miCakeModules)
            {
                _logger.LogInformation($"MiCake LiftTime-Start:{ miCakeModule.Type.Name }");

                var miCakeLiftTime = (MiCakeModule)miCakeModule.ModuleInstance;
                miCakeLiftTime.Start(moduleContext);
            }

            _configureModule?.Invoke(_miCakeModules);

            //OnStart
            foreach (var miCakeModule in _miCakeModules)
            {
                _logger.LogInformation($"MiCake LiftTime-OnStart:{ miCakeModule.Type.Name }");

                var miCakeLiftTime = (MiCakeModule)miCakeModule.ModuleInstance;
                miCakeLiftTime.OnStart(moduleContext);
            }

            _logger.LogInformation("MiCake:InitializeModules Completed");
        }

        public virtual void ShutDownModules()
        {
            var moduleContext = new ModuleContext(_services, _miCakeModules);

            _logger.LogInformation("MiCake:ShutDownModules...");

            var reverseModules = _miCakeModules.Reverse().ToList();
            //PreShuntdown
            foreach (var miCakeModule in reverseModules)
            {
                _logger.LogInformation($"MiCake LiftTime-PreStart:{ miCakeModule.Type.Name }");

                var miCakeLiftTime = (MiCakeModule)miCakeModule.ModuleInstance;
                miCakeLiftTime.PreShuntdown(moduleContext);
            }

            //Shuntdown
            foreach (var miCakeModule in reverseModules)
            {
                _logger.LogInformation($"MiCake LiftTime-Start:{ miCakeModule.Type.Name }");

                var miCakeLiftTime = (MiCakeModule)miCakeModule.ModuleInstance;
                miCakeLiftTime.Shuntdown(moduleContext);
            }

            //OnShuntdown
            foreach (var miCakeModule in reverseModules)
            {
                _logger.LogInformation($"MiCake LiftTime-OnShuntdown:{ miCakeModule.Type.Name }");

                var miCakeLiftTime = (MiCakeModule)miCakeModule.ModuleInstance;
                miCakeLiftTime.OnShuntdown(moduleContext);
            }

            _logger.LogInformation("MiCake:InitializeModules Completed");
        }

        private IEnumerable<MiCakeModuleDescriptor> ResolvingMiCakeModule(Type startUp)
        {
            List<MiCakeModuleDescriptor> miCakeModuleDescriptors = new List<MiCakeModuleDescriptor>();

            var moduleTypes = MiCakeModuleHelper.FindAllModuleTypes(startUp);
            foreach (var moduleTye in moduleTypes)
            {
                MiCakeModule instance = (MiCakeModule)Activator.CreateInstance(moduleTye);
                miCakeModuleDescriptors.Add(new MiCakeModuleDescriptor(moduleTye, instance));

                _services.AddSingleton(moduleTye, instance);
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

        public virtual IMiCakeModuleEngine ConfigureModule(Action<IMiCakeModuleCollection> configureModule)
        {
            _configureModule += configureModule;
            return this;
        }
    }
}
