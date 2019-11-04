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
        /// <summary>
        /// Load and resolving micake module.
        /// 加载和解析Micake模块。
        /// </summary>
        /// <param name="startUpModule">初始化模块入口点</param>
        /// <returns></returns>
        IEnumerable<MiCakeModuleDescriptor> LoadMiCakeModules(Type startUpModule);

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
