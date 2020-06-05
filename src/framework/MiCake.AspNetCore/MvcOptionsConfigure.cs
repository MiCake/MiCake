using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.AspNetCore.Security;
using MiCake.AspNetCore.Uow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MiCake.AspNetCore
{
    /// <summary>
    /// Use to add filter to aspnetcore mvc
    /// </summary>
    internal class MvcOptionsConfigure : IConfigureOptions<MvcOptions>
    {
        private MiCakeAspNetOptions _micakeAspNetOptions;

        public MvcOptionsConfigure(IOptions<MiCakeAspNetOptions> micakeAspNetOptions)
        {
            _micakeAspNetOptions = micakeAspNetOptions.Value;
        }

        public void Configure(MvcOptions options)
        {
            options.Filters.Add(typeof(UnitOfWorkFilter));

            //Add Data wrapper filters
            if (_micakeAspNetOptions.UseDataWrapper)
            {
                options.Filters.Add(typeof(DataWrapperFilter));
                options.Filters.Add(typeof(ExceptionDataWrapperFilter));

                //Security
                options.Filters.Add(new VerifyCurrentUserFilter());   //not required some services,use instance directly.
            }
        }
    }
}
