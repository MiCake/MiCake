using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.DependencyInjection
{
    /// <summary>
    /// Callback event at service injection
    /// 注入服务时的回调事件
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="callBackContext">injectType 被注入的类型上下文</param>
    public delegate void InjectServiceRegistCallback(IServiceCollection services, InjectServiceCallBackContext callBackContext);
}
