using MiCake.Uow;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.AspNetCore
{
    public class MiCakeAspNetOptions
    {
        public MiCakeAspNetUowOption UnitOfWork { get; set; }

        public MiCakeAspNetOptions()
        {
            UnitOfWork = new MiCakeAspNetUowOption();
        }
    }

    /// <summary>
    /// Provides configuration for the MiCake UnitOfWork.
    /// </summary>
    public class MiCakeAspNetUowOption
    {
        /// <summary>
        /// <see cref="UnitOfWorkOptions"/> for root <see cref="IUnitOfWork"/>
        /// MiCake always has a Root UnitOfWork. It provider your db transaction object.
        /// if you dont want to open transaction default,set this options or use other ways.
        /// </summary>
        public UnitOfWorkOptions? RootUowOptions { get; set; }

        /// <summary>
        /// Match controller action name start key work to close unit of work transaction.
        /// default is : [Find],[Get],[Query]
        /// 
        /// it taks effect at <see cref="UnitOfWorkLimit"/> is not Suppress 
        /// and has no <see cref="DisableTransactionAttribute"/>
        /// </summary>
        public List<string> KeyWordToCloseTransaction { get; set; }
            = new List<string>() { "Find", "Get", "Query" };
    }
}
