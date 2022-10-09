using MiCake.AspNetCore.DataWrapper.Internals;
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
        private readonly MiCakeAspNetCoreOptions _micakeAspNetOptions;


        public MvcOptionsConfigure(IOptions<MiCakeAspNetCoreOptions> micakeAspNetOptions)
        {
            _micakeAspNetOptions = micakeAspNetOptions.Value;
        }

        public void Configure(MvcOptions options)
        {
            options.Filters.Add(typeof(UnitOfWorkAutoSaveFilter));

            //Add wrap data filters.
            if (_micakeAspNetOptions.WrapResponseAndPureExceptionData)
            {
                options.Filters.Add(typeof(DataWrapperFilter));
                options.Filters.Add(typeof(ExceptionDataWrapperFilter));
            }
        }
    }
}
