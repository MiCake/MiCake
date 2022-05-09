using MiCake.Core.Util.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Description of the Mike module <see cref=" MiCakeModule "/>, including details of the module
    /// </summary>
    [DebuggerDisplay("{ModuleType.Name}.Rely On {RelyOnModules.Count}")]
    public class MiCakeModuleDescriptor
    {
        /// <summary>
        /// A instance for this <see cref="MiCakeModule"/>.
        /// </summary>
        public MiCakeModule Instance { get; }

        /// <summary>
        /// The type of this module.
        /// </summary>
        public Type ModuleType { get; }

        /// <summary>
        /// The Assembly of this module.
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// Indicate this module has <see cref="CoreModuleAttribute"/> tag.
        /// </summary>
        public bool IsCoreModule { get; }

        /// <summary>
        /// Other modules that this module depends on.
        /// </summary>
        public IReadOnlyList<MiCakeModuleDescriptor> RelyOnModules => _relyOnModules.ToImmutableList();
        private readonly List<MiCakeModuleDescriptor> _relyOnModules;

        public MiCakeModuleDescriptor(Type type, MiCakeModule instance)
        {
            Instance = instance;
            ModuleType = type;
            Assembly = type.Assembly;
            _relyOnModules = new List<MiCakeModuleDescriptor>();
            IsCoreModule = type.GetCustomAttribute<CoreModuleAttribute>() != null;
        }

        public void AddDependency(MiCakeModuleDescriptor descriptor)
        {
            _relyOnModules.AddIfNotContains(descriptor);
        }
    }
}
