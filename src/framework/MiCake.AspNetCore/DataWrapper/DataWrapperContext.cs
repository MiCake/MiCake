using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MiCake.AspNetCore.DataWrapper
{
    public class DataWrapperContext
    {
        /// <summary>
        ///  Gets <see cref="ActionDescriptor"/> for the selected action.
        ///  
        ///  It's maybe null
        /// </summary>
        public ActionDescriptor ActionDescriptor { get; }

        /// <summary>
        /// The data returned after the action is executed.
        /// The data has not been wrapped
        /// </summary>
        public object ResultData { get; }

        /// <summary>
        /// <see cref="HttpContext"/>
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// <see cref="ModelStateDictionary"/>
        /// 
        /// It's maybe null
        /// </summary>
        public ModelStateDictionary ModelState { get; }

        /// <summary>
        /// <see cref="DataWrapperOptions"/>
        /// </summary>
        public DataWrapperOptions WrapperOptions { get; }


        public DataWrapperContext(object resultData,
                                  HttpContext httpContext,
                                  DataWrapperOptions options,
                                  ActionDescriptor actionDescriptor = null,
                                  ModelStateDictionary modelstate = null)
        {
            ResultData = resultData;
            WrapperOptions = options;
            HttpContext = httpContext;
            ActionDescriptor = actionDescriptor;
            ModelState = modelstate;
        }
    }
}
