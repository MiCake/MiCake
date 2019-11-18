using MiCake.Core.Abstractions.Modularity;

namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Manage the services that need to be injected automatically in MiCake
    /// </summary>
    internal interface IMiCakeDIManager
    {
        /// <summary>
        /// inject the service that impletement <see cref="IAutoInjectService"/>
        /// 注入micake module中实现了自动注入接口的服务
        /// </summary>
        void PopulateAutoService(IMiCakeModuleCollection miCakeModules);
    }
}
