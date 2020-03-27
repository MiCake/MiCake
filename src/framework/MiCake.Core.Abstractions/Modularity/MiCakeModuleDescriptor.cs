using MiCake.Core.Util.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Description of the Mike module <see cref=" MiCakeModule "/>, including details of the module
    /// </summary>
    public class MiCakeModuleDescriptor
    {
        public MiCakeModule Instance { get; }

        public Type Type { get; }

        public Assembly Assembly { get; }

        public IReadOnlyList<MiCakeModuleDescriptor> Dependencies => _dependencies.ToImmutableList();
        private readonly List<MiCakeModuleDescriptor> _dependencies;

        public MiCakeModuleDescriptor(Type type, MiCakeModule instance)
        {
            Instance = instance;
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
