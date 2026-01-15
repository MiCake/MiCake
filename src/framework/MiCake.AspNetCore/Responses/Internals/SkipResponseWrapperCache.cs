using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using MiCake.Util.Cache;

namespace MiCake.AspNetCore.Responses.Internals
{
    /// <summary>
    /// Cache for skip response wrapper attribute checks.
    /// Uses a small bounded LRU cache to avoid reflection on each request.
    /// </summary>
    internal static class SkipResponseWrapperCache
    {
        // Small cache capacity sufficient for typical applications.
        // Use 1024 to balance memory and hit rate; adjust if needed.
        private const int MethodCacheSize = 1024;
        private const int ControllerCacheSize = 256;

        private static readonly BoundedLruCache<MethodInfo, bool> _methodHasAttribute
            = new BoundedLruCache<MethodInfo, bool>(MethodCacheSize);

        private static readonly BoundedLruCache<Type, bool> _controllerHasAttribute
            = new BoundedLruCache<Type, bool>(ControllerCacheSize);

        public static bool ShouldSkip(ControllerActionDescriptor cad)
        {
            if (cad == null)
                return false;

            var method = cad.MethodInfo;
            if (method != null)
            {
                // Cache boolean existence using Attribute.IsDefined to avoid allocating the attribute instance
                var methodHas = _methodHasAttribute.GetOrAdd(method, m => Attribute.IsDefined(m, typeof(SkipResponseWrapperAttribute), inherit: true));
                if (methodHas)
                    return true;
            }

            var controllerType = cad.ControllerTypeInfo?.AsType();
            if (controllerType != null)
            {
                var controllerHas = _controllerHasAttribute.GetOrAdd(controllerType, t => Attribute.IsDefined(t, typeof(SkipResponseWrapperAttribute), inherit: true));
                if (controllerHas)
                    return true;
            }

            return false;
        }
    }
}
