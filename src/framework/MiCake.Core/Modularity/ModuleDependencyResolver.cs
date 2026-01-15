using System;
using System.Collections.Generic;
using System.Linq;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Module dependency resolver using topological sorting (Kahn's algorithm).
    /// Provides clear dependency graph representation and circular dependency detection.
    /// </summary>
    internal class ModuleDependencyResolver
    {
        private readonly Dictionary<Type, ModuleNode> _moduleNodes = [];
        private bool _isGraphBuilt = false;
        private List<MiCakeModuleDescriptor>? _cachedLoadOrder;

        /// <summary>
        /// Registers a module descriptor for dependency resolution.
        /// </summary>
        /// <param name="descriptor">The module descriptor to register</param>
        /// <exception cref="ArgumentNullException">When descriptor is null</exception>
        public void RegisterModule(MiCakeModuleDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);

            var node = new ModuleNode(descriptor);
            _moduleNodes[descriptor.ModuleType] = node;

            // Invalidate cache when new module is registered
            _isGraphBuilt = false;
            _cachedLoadOrder = null;
        }

        /// <summary>
        /// Gets the dependency graph as a formatted string showing module relationships.
        /// Format: ModuleA -> ModuleB -> ModuleC
        /// </summary>
        /// <returns>A string representation of the dependency graph in load order</returns>
        public string GetDependencyGraph()
        {
            if (_moduleNodes.Count == 0)
                return string.Empty;

            // Use cached or compute load order to ensure consistency
            var loadOrder = _cachedLoadOrder ?? ResolveLoadOrder();

            // Build module names with framework indicator
            var moduleNames = loadOrder.Select(descriptor =>
            {
                var moduleName = descriptor.ModuleType?.Name ?? "Unknown";
                var isFrameworkLevel = descriptor.Instance.IsFrameworkLevel;
                return isFrameworkLevel ? $"*{moduleName}" : moduleName;
            });

            return string.Join(" -> ", moduleNames);
        }

        /// <summary>
        /// Resolves the module load order using topological sorting.
        /// Uses Kahn's algorithm for clear and efficient dependency resolution.
        /// </summary>
        /// <returns>A sorted list of module descriptors in dependency order</returns>
        /// <exception cref="InvalidOperationException">When circular dependency is detected</exception>
        public List<MiCakeModuleDescriptor> ResolveLoadOrder()
        {
            // Return cached result if available
            if (_cachedLoadOrder != null)
                return _cachedLoadOrder;

            if (!_isGraphBuilt)
                BuildDependencyGraph();

            // Use Kahn's algorithm for topological sorting with priority queue
            var sorted = new List<MiCakeModuleDescriptor>();
            var inDegree = CalculateInDegree();

            var readyNodes = new List<ModuleNode>();

            // Find all nodes with in-degree 0 (no dependencies)
            foreach (var (_, node) in _moduleNodes)
            {
                if (inDegree[node] == 0)
                {
                    readyNodes.Add(node);
                }
            }

            // Process nodes in topological order with priority
            while (readyNodes.Count > 0)
            {
                // Sort by priority: Framework modules first, with MiCakeRootModule at the very top
                var node = GetHighestPriorityNode(readyNodes);
                readyNodes.Remove(node);
                sorted.Add(node.Descriptor);

                // Reduce in-degree for all dependents
                foreach (var dependent in node.Dependents)
                {
                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0)
                    {
                        readyNodes.Add(dependent);
                    }
                }
            }

            // Detect circular dependencies
            if (sorted.Count != _moduleNodes.Count)
            {
                var unprocessedModules = _moduleNodes.Values
                    .Where(n => !sorted.Contains(n.Descriptor))
                    .Select(n => n.Descriptor.ModuleType.Name);

                throw new InvalidOperationException(
                    $"Circular module dependency detected. Affected modules: {string.Join(", ", unprocessedModules)}");
            }

            // Cache the result
            _cachedLoadOrder = sorted;
            return sorted;
        }

        /// <summary>
        /// Selects the highest priority node from the ready list.
        /// Priority order: MiCakeRootModule > Other Framework modules > Regular modules
        /// This ensures framework modules are always loaded before application modules when there are no dependency constraints.
        /// </summary>
        private static ModuleNode GetHighestPriorityNode(List<ModuleNode> readyNodes)
        {
            if (readyNodes.Count == 1)
                return readyNodes[0];

            // First priority: MiCakeRootModule
            var rootModule = readyNodes.FirstOrDefault(n => n.Descriptor.ModuleType.Name == nameof(MiCakeRootModule));
            if (rootModule != null)
                return rootModule;

            // Second priority: Other framework-level modules
            var frameworkModule = readyNodes.FirstOrDefault(n => n.Descriptor.Instance.IsFrameworkLevel);
            if (frameworkModule != null)
                return frameworkModule;

            // Default: Return first regular module
            return readyNodes[0];
        }

        /// <summary>
        /// Builds the dependency graph by linking module nodes with their dependencies.
        /// </summary>
        private void BuildDependencyGraph()
        {
            foreach (var (_, node) in _moduleNodes)
            {
                var dependencyTypes = MiCakeModuleHelper.FindDependedModuleTypes(node.Descriptor.ModuleType);

                foreach (var dependencyType in dependencyTypes)
                {
                    if (_moduleNodes.TryGetValue(dependencyType, out var dependencyNode))
                    {
                        node.AddDependency(dependencyNode);
                    }
                }
            }

            _isGraphBuilt = true;
        }

        /// <summary>
        /// Calculates the in-degree (number of dependencies) for each node.
        /// </summary>
        /// <returns>A dictionary mapping each node to its in-degree</returns>
        private Dictionary<ModuleNode, int> CalculateInDegree()
        {
            var inDegree = new Dictionary<ModuleNode, int>();

            // Initialize all nodes with in-degree 0
            foreach (var (_, node) in _moduleNodes)
            {
                inDegree[node] = 0;
            }

            // Count dependencies (in-degree)
            foreach (var (_, node) in _moduleNodes)
            {
                foreach (var dependency in node.Dependencies)
                {
                    inDegree[node]++;
                }
            }

            return inDegree;
        }

        /// <summary>
        /// Represents a node in the module dependency graph.
        /// </summary>
        private sealed class ModuleNode
        {
            /// <summary>
            /// Gets the module descriptor for this node.
            /// </summary>
            public MiCakeModuleDescriptor Descriptor { get; }

            /// <summary>
            /// Gets the list of modules this module depends on.
            /// </summary>
            public List<ModuleNode> Dependencies { get; } = [];

            /// <summary>
            /// Gets the list of modules that depend on this module.
            /// </summary>
            public List<ModuleNode> Dependents { get; } = [];

            public ModuleNode(MiCakeModuleDescriptor descriptor)
            {
                Descriptor = descriptor;
            }

            /// <summary>
            /// Adds a dependency relationship between this node and another.
            /// </summary>
            /// <param name="dependency">The node this module depends on</param>
            public void AddDependency(ModuleNode dependency)
            {
                if (!Dependencies.Contains(dependency))
                {
                    Dependencies.Add(dependency);
                    dependency.Dependents.Add(this);
                }
            }
        }
    }
}
