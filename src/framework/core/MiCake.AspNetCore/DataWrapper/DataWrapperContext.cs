using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace MiCake.AspNetCore.DataWrapper
{
    public class DataWrapperContext
    {
        /// <summary>
        ///  Gets <see cref="ActionDescriptor"/> for the selected action.
        /// </summary>
        public ActionDescriptor? ActionDescriptor { get; set; }

        /// <summary>
        /// The data returned after the action is executed.
        /// <see cref="IActionResult"/>
        /// </summary>
        public IActionResult ResultData { get; set; }

        /// <summary>
        /// <see cref="HttpContext"/>
        /// </summary>
        public HttpContext HttpContext { get; set; }


        public DataWrapperContext(IActionResult resultData,
                                  HttpContext httpContext,
                                  ActionDescriptor? actionDescriptor = null)
        {
            ResultData = resultData;
            HttpContext = httpContext;
            ActionDescriptor = actionDescriptor;
        }
    }
}
