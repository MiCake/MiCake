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
        }

        /// <summary>
        /// Resolves the module load order using topological sorting.
        /// Uses Kahn's algorithm for clear and efficient dependency resolution.
        /// </summary>
        /// <returns>A sorted list of module descriptors in dependency order</returns>
        /// <exception cref="InvalidOperationException">When circular dependency is detected</exception>
        public List<MiCakeModuleDescriptor> ResolveLoadOrder()
        {
            // Build dependency graph
            BuildDependencyGraph();

            // Use Kahn's algorithm for topological sorting
            var sorted = new List<MiCakeModuleDescriptor>();
            var inDegree = CalculateInDegree();
            var queue = new Queue<ModuleNode>();

            // Find all nodes with in-degree 0 (no dependencies)
            foreach (var (_, node) in _moduleNodes)
            {
                if (inDegree[node] == 0)
                {
                    queue.Enqueue(node);
                }
            }

            // Process nodes in topological order
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                sorted.Add(node.Descriptor);

                // Reduce in-degree for all dependents
                foreach (var dependent in node.Dependents)
                {
                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
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

            return sorted;
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
        private class ModuleNode
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
