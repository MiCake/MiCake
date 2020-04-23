using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// The options of wrapper reponse data.
    /// </summary>
    public class DataWrapperOptions
    {
        /// <summary>
        /// Shows the stack trace information in the responseException details.
        /// </summary>
        public bool IsDebug { get; set; } = false;

        /// <summary>
        /// When the http status code in this list, the result is not wrapped.
        /// Defatult:204(No Content),301(Redirect),302(Redirect),304(NotModified)
        /// 
        /// <see cref="StatusCodes"/>
        /// </summary>
        public List<int> NoWrapStatusCode { get; set; } = new List<int>() { 204, 301, 302, 304 };

        /// <summary>
        /// Use custom return data model or not.
        /// If this property is true,you must config <see cref="CustomerProperty"/>.Otherwise,the response data is original.
        /// </summary>
        public bool UseCustomModel { get; set; } = false;

        /// <summary>
        /// Custom returned property item
        /// Use <see cref="ConfigWrapperPropertyDelegate"/> to get data.
        /// <para>
        ///     example:CompanyName = s => "MiCake";
        /// </para>
        /// </summary>
        public Dictionary<string, ConfigWrapperPropertyDelegate> CustomerProperty { get; set; }
    }
}
