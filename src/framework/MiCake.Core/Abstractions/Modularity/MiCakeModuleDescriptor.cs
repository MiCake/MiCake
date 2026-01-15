using MiCake.Util.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Description of the Mike module <see cref=" MiCakeModule "/>, including details of the module
    /// </summary>
    [DebuggerDisplay("{ModuleType.Name}.Rely On {RelyOnModules.Count}")]
    public class MiCakeModuleDescriptor(Type type, MiCakeModule instance)
    {
        /// <summary>
        /// A instance for this <see cref="MiCakeModule"/>.
        /// </summary>
        public MiCakeModule Instance { get; } = instance;

        /// <summary>
        /// The type of this module.
        /// </summary>
        public Type ModuleType { get; } = type;

        /// <summary>
        /// The Assembly of this module.
        /// </summary>
        public Assembly Assembly { get; } = type.Assembly;

        /// <summary>
        /// Other modules that this module depends on
        /// </summary>
        public IReadOnlyList<MiCakeModuleDescriptor> RelyOnModules => [.. _relyOnModules];
        private readonly List<MiCakeModuleDescriptor> _relyOnModules = [];

        public void AddDependency(MiCakeModuleDescriptor descriptor)
        {
            _relyOnModules.AddIfNotContains(descriptor);
        }
    }
}
