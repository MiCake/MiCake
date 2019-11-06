using MiCake.Core.Abstractions.Modularity;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.DependencyInjection
{
    public interface IMiCakeDIManager
    {
        /// <summary>
        /// inject the service that impletement <see cref="IAutoInjectService"/>
        /// 注入micake module中实现了自动注入接口的服务
        /// </summary>
        void PopulateAutoService(IMiCakeModuleCollection miCakeModules);
    }
}
