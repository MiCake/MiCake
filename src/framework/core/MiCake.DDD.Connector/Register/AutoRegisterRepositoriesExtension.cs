using MiCake.Core.Util.Reflection;
using MiCake.DDD.Domain;
using System.Reflection;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// A selector used to filter custom warehouse interfaces,Used to <see cref="AutoRegisterRepositoriesExtension"/>.
    /// </summary>
    /// <param name="repoType">Current repository type.</param>
    /// <param name="repoInterfaceType">Interface type inherited by current repository.</param>
    /// <param name="currentIndex">Current index for interface.(usually,the higher the level of the interface, the greater the value)</param>
    /// <returns></returns>
    public delegate bool CustomRepositorySelector(Type repoType, Type repoInterfaceType, int currentIndex);

    public static class AutoRegisterRepositoriesExtension
    {
        /// <summary>
        /// Automatically scan and register all repositories within an assembly.use default selector.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="assembly">The assembly in which the custom repository resides</param>
        public static void AutoRegisterRepositories(this ModuleConfigServiceContext context, Assembly assembly)
        {
            context.AutoRegisterRepositories(assembly, (repo, repoInterface, index) =>
            {
                return repoInterface.Name.Contains(repo.Name);
            });
        }

        /// <summary>
        /// Automatically scan and register all repositories within an assembly.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="assembly">The assembly in which the custom repository resides</param>
        /// <param name="selector">a selector,see <see cref="CustomRepositorySelector"/>.</param>
        public static void AutoRegisterRepositories(this ModuleConfigServiceContext context, Assembly assembly, CustomRepositorySelector selector)
        {
            var allRepoTypes = assembly.GetTypes().Where(s => TypeHelper.IsConcrete(s) && typeof(IRepository).IsAssignableFrom(s));

            List<(Type repoInterface, Type repo)> result = new();

            foreach (var repo in allRepoTypes)
            {
                var interfaces = repo.GetInterfaces().Where(s => !s.Name.Contains(nameof(IRepository))).Reverse();

                int tempIndex = 0;
                foreach (var repoInterface in interfaces)
                {
                    if (selector(repo, repoInterface, tempIndex))
                        result.Add((repoInterface, repo));

                    tempIndex++;
                }
            }

            result.ForEach(s =>
            {
                context.RegisterRepository(s.repoInterface, s.repo);
            });
        }
    }
}
