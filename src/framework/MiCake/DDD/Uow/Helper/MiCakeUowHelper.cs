using System;
using System.Reflection;

namespace MiCake.DDD.Uow.Helper
{
    public static class MiCakeUowHelper
    {
        public static bool IsDisableTransaction(Type markedClassType)
        {
            return markedClassType.GetCustomAttribute<DisableTransactionAttribute>() != null;
        }

        public static bool IsDisableTransaction(MethodInfo markedMethodInfo)
        {
            return markedMethodInfo.GetCustomAttribute<DisableTransactionAttribute>() != null;
        }

        public static bool IsMiCakeUnitOfWork(Type type)
        {
            return typeof(IUnitOfWork).IsAssignableFrom(type);
        }
    }
}
