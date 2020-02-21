using System;
using System.Reflection;

namespace MiCake.EntityFrameworkCore
{
    public class MiCakeEFCoreOptions
    {
        public bool RegisterDefaultRepository { get; set; } = true;

        /// <summary>
        /// Place the assembly of the domain object
        /// If it is not indicated that the assembly system will traverse all user module lookups
        /// </summary>
        public Assembly[] DomainObjectAssembly { get; set; }

        public bool RegisterFreeRepository { get; set; } = false;

        public Type DbContextType { get; private set; }

        public MiCakeEFCoreOptions(Type dbContextType)
        {
            DbContextType = dbContextType;
        }
    }
}
