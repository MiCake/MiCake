using System;

namespace MiCake.DDD.Uow.Helper
{
    public static class MiCakeUowHelper
    {
        public static bool IsMiCakeUnitOfWork(Type type)
        {
            return typeof(IUnitOfWork).IsAssignableFrom(type);
        }
    }
}
