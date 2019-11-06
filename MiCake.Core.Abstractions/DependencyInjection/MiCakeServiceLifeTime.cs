using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.Core.Abstractions.DependencyInjection
{
    /// <summary>
    /// Specifies the lifetime of a service
    /// </summary>
    public enum MiCakeServiceLifeTime
    {
        //
        // 摘要:
        //     Specifies that a single instance of the service will be created.
        Singleton = 0,
        //
        // 摘要:
        //     Specifies that a new instance of the service will be created for each scope.
        //
        // 言论：
        //     In ASP.NET Core applications a scope is created around each server request.
        Scoped = 1,
        //
        // 摘要:
        //     Specifies that a new instance of the service will be created every time it is
        //     requested.
        Transient = 2
    }
}
