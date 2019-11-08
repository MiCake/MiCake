using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    /// <summary>
    /// MiCakeModule Engine,Create and manage modules and handle dependencies between them
    /// MiCakeModule引擎。负责创建和管理模块 以及处理他们之间的依赖关系
    /// </summary>
    public interface IMiCakeModuleEngine
    {
        IMiCakeModuleCollection MiCakeModules { get; }

        /// <summary>
        /// Load and resolving micake module.
        /// 加载和解析Micake模块。
        /// </summary>
        /// <param name="startUpModule">初始化模块入口点</param>
        /// <returns></returns>
        IEnumerable<MiCakeModuleDescriptor> LoadMiCakeModules(Type startUpModule);

        /// <summary>
        /// Add module configuration delegation, which will be called when the engine Initialize
        /// 加入模块配置的委托，将在引擎加载时候调用.
        /// </summary>
        IMiCakeModuleEngine ConfigureModule(Action<IMiCakeModuleCollection> configureModule);

        /// <summary>
        /// Load All Micake Modules
        /// 加载所有模块
        /// </summary>
        void InitializeModules();

        /// <summary>
        /// ShutDown All Micak Modules
        /// 关闭所有模块
        /// </summary>
        void ShutDownModules();
    }
}
