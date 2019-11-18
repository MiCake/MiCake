using MiCake.Core.Abstractions.Builder;
using MiCake.Core.Abstractions.Modularity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions
{
    public interface IMiCakeApplication : IDisposable
    {
        Type StartUpType { get; }

        IMiCakeBuilder Builder { get; }

        void Init();

        /// <summary>
        /// To configure Mike's extension, you can use <see cref="IMiCakeBuilder"/> to add services to <see cref="IServiceCollection"/>
        /// 
        /// 配置Micake的扩展，可以利用IMiCakeBuilder向IServiceCollection中添加服务.
        /// </summary>
        IMiCakeApplication Configure(Action<IMiCakeBuilder> builderConfigAction);

        /// <summary>
        /// Used to gracefully shutdown the application and all modules.
        /// </summary>
        void ShutDown(Action<ModuleBearingContext> shutdownAction = null);
    }
}
