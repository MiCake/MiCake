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
        /// Custom returned property item
        /// Use <see cref="ConfigWrapperPropertyDelegate"/> to get data.
        /// <para>
        ///     example:CompanyName = s => "MiCake";
        /// </para>
        /// </summary>
        public Dictionary<string, ConfigWrapperPropertyDelegate> CustomerProperty { get; set; }
    }
}
