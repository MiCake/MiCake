using System.Reflection;

namespace MiCake.Uow.Helper
{
    public static class MiCakeUowHelper
    {
        public static bool IsDisableAutoUow(Type markedClassType)
        {
            return markedClassType.GetCustomAttribute<DisableAutoUnitOfWorkAttribute>() != null;
        }

        public static bool IsDisableAutoUow(MethodInfo markedMethodInfo)
        {
            return markedMethodInfo.GetCustomAttribute<DisableAutoUnitOfWorkAttribute>() != null;
        }

        public static bool IsEnableAutoUow(Type markedClassType)
        {
            return markedClassType.GetCustomAttribute<AutoUnitOfWorkAttribute>() != null;
        }

        public static bool IsEnableAutoUow(MethodInfo markedMethodInfo)
        {
            return markedMethodInfo.GetCustomAttribute<AutoUnitOfWorkAttribute>() != null;
        }

        public static bool IsMiCakeUnitOfWork(Type type)
        {
            return typeof(IUnitOfWork).IsAssignableFrom(type);
        }
    }
}
