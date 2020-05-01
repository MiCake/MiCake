using MiCake.AspNetCore.DataWrapper;
using MiCake.Uow;
using System.Collections.Generic;

namespace MiCake.AspNetCore
{
    /// <summary>
    /// The options for micake asp net core.
    /// </summary>
    public class MiCakeAspNetOptions
    {
        /// <summary>
        /// The unit of work config for micake in asp net core.
        /// </summary>
        public MiCakeAspNetUowOption UnitOfWork { get; set; }

        /// <summary>
        /// Whether it is need to format the returned data.
        /// When you choose true, you can also customize the configuration by <see cref="DataWrapperOptions"/>
        /// </summary>
        public bool UseDataWrapper { get; set; } = true;

        /// <summary>
        /// The data wrap config for micake in asp net core.
        /// </summary>
        public DataWrapperOptions DataWrapperOptions { get; set; }

        public MiCakeAspNetOptions()
        {
            UnitOfWork = new MiCakeAspNetUowOption();
            DataWrapperOptions = new DataWrapperOptions();
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
