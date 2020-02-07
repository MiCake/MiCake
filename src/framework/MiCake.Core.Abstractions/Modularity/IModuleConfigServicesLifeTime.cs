using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.Modularity
{
    /// <summary>
    /// 
    /// Configure the lifecycle of the module service
    /// When the module is initialized, it is called in turn according to the calling order.
    /// 
    /// 配置模块服务的生命周期.
    /// 在模块初始化时，根据调用顺序依次调用.
    /// 
    /// </summary>
    internal interface IModuleConfigServicesLifeTime
    {
        void PreConfigServices(ModuleConfigServiceContext context);

        void ConfigServices(ModuleConfigServiceContext context);

        void PostConfigServices(ModuleConfigServiceContext context);
    }
}
