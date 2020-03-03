using MiCake.Core.DependencyInjection;
using System;

namespace MiCake.EntityFrameworkCore
{
    public class MiCakeEFCoreOptions : IObjectAccessor<MiCakeEFCoreOptions>
    {
        public bool RegisterDefaultRepository { get; set; } = true;

        public bool RegisterFreeRepository { get; set; } = false;

        public Type DbContextType { get; private set; }

        MiCakeEFCoreOptions IObjectAccessor<MiCakeEFCoreOptions>.Value => this;

        public MiCakeEFCoreOptions(Type dbContextType)
        {
            DbContextType = dbContextType;
        }
    }
}
