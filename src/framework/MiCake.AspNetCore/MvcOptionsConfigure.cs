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
        public void Configure(MvcOptions options)
        {
            options.Filters.Add(typeof(UnitOfWorkFilter));
            //Add Data wrapper filters
            options.Filters.Add(typeof(DataWrapperFilter));
            options.Filters.Add(typeof(ExceptionDataWrapper));
        }
    }
}
