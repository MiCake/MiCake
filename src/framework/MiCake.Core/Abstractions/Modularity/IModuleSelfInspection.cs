using System.Threading.Tasks;

namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Defined that the module has self inspection function.
    /// Self inspection will be carried out when the MiCake application is started.
    /// </summary>
    public interface IModuleSelfInspection
    {
        /// <summary>
        /// Execute the logic of self inspection. If there is any error, throw the corresponding exception
        /// </summary>
        /// <param name="context"><see cref="ModuleInspectionContext"/></param>
        Task Inspect(ModuleInspectionContext context);
    }
}
