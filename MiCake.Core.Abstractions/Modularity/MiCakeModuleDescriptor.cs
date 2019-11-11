using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections.Immutable;
using MiCake.Core.Util.Collections;

namespace MiCake.Core.Abstractions.Modularity
{
    /// <summary>
    /// MiCake 模块<see cref=" MiCakeModule "/>的描述信息，包含模块的详细信息 
    /// Description of the Mike module <see cref=" MiCakeModule "/>, including details of the module
    /// </summary>
    public class MiCakeModuleDescriptor
    {
        public MiCakeModule ModuleInstance { get; }

        public Type Type { get; }

        public Assembly Assembly { get; }

        public IReadOnlyList<MiCakeModuleDescriptor> Dependencies => _dependencies.ToImmutableList();
        private readonly List<MiCakeModuleDescriptor> _dependencies;

        public MiCakeModuleDescriptor(Type type, MiCakeModule instance)
        {
            ModuleInstance = instance;
            Type = type;
            Assembly = type.Assembly;
            _dependencies = new List<MiCakeModuleDescriptor>();
        }

        public void AddDependency(MiCakeModuleDescriptor descriptor)
        {
            _dependencies.AddIfNotContains(descriptor);
        }
    }
}
