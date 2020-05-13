using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace MiCake.AspNetCore.DataWrapper
{
    /// <summary>
    /// The options of wrap reponse data.
    /// </summary>
    public class DataWrapperOptions
    {
        /// <summary>
        /// Shows the stack trace information in the responseException details.
        /// </summary>
        public bool IsDebug { get; set; } = false;

        /// <summary>
        /// When the http status code in this list, the result is not wrapped.
        /// Defatult:201,202,404
        /// 
        /// <see cref="StatusCodes"/>
        /// </summary>
        public List<int> NoWrapStatusCode { get; set; } = new List<int>() { 201, 202, 404 };

        /// <summary>
        /// <see cref="ProblemDetails"/> has a separate format in asp net core.
        /// If this values is true,ProblemDetails will be wrapped.So you will lost some error info.
        /// <para>
        ///     Default :false.
        /// </para>
        /// </summary>
        public bool WrapProblemDetails { get; set; } = false;

        /// <summary>
        /// Use custom return data model or not.
        /// If this property is true,you must config <see cref="CustomModelConfig"/>.Otherwise,the response data is original.
        /// </summary>
        public bool UseCustomModel { get; set; } = false;

        /// <summary>
        /// Return custom wrapper model according to HTTP status code.
        /// <para>
        ///     For example:Key: 200..400 Value:MyCustomerModel.
        ///     This means that when the HTTP status code is between 200 and 400, MyCustomerModel will be used as the return wrapper type.
        /// </para>
        /// <para>
        ///     The precondition for executing the wrapper is: 1: the result source type is <see cref="ObjectResult"/> 2: no exception occurred.
        /// </para>
        /// </summary>
        public Dictionary<Range, CustomWrapperModel> CustomModelConfig { get; set; }
    }
}
