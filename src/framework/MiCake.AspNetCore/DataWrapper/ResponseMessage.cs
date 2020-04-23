using System;
using System.Collections.Generic;
using System.Text;

namespace MiCake.AspNetCore.DataWrapper
{
    internal class ResponseMessage
    {
        internal const string Success = "Request successful.";
        internal const string NotFound = "Request not found. The specified uri does not exist.";
        internal const string BadRequest = "Request invalid.";
        internal const string MethodNotAllowed = "Request responded with 'Method Not Allowed'.";
        internal const string Exception = "Request responded with exceptions.";
        internal const string UnAuthorized = "Request denied. Unauthorized access.";
        internal const string ValidationError = "Request responded with one or more validation errors occurred.";
        internal const string Unknown = "Request cannot be processed. Please contact support.";
        internal const string Unhandled = "Unhandled Exception occurred. Unable to process the request.";
    }
}
