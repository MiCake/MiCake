using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace MiCake.AspNetCore.ApiLogging.Internals
{
    /// <summary>
    /// Default implementation of <see cref="IApiLogEntryFactory"/>.
    /// </summary>
    internal sealed class DefaultApiLogEntryFactory : IApiLogEntryFactory
    {
        public ApiLogEntry CreateEntry(HttpContext httpContext, ApiLoggingEffectiveConfig configuration)
        {
            var request = httpContext.Request;

            var entry = new ApiLogEntry
            {
                CorrelationId = httpContext.TraceIdentifier,
                Timestamp = DateTimeOffset.UtcNow,
                Request = new ApiRequestLog
                {
                    Method = request.Method,
                    Path = request.Path.Value ?? string.Empty,
                    QueryString = request.QueryString.HasValue ? request.QueryString.Value : null,
                    ContentType = request.ContentType,
                    ContentLength = request.ContentLength
                }
            };

            // Capture request headers if enabled
            if (configuration.LogRequestHeaders)
            {
                entry.Request.Headers = CaptureHeaders(request.Headers);
            }

            return entry;
        }

        public void PopulateResponse(
            ApiLogEntry entry,
            HttpContext httpContext,
            ApiLoggingEffectiveConfig configuration,
            string? responseBody,
            TimeSpan elapsed)
        {
            var response = httpContext.Response;

            entry.ElapsedMilliseconds = (long)elapsed.TotalMilliseconds;
            entry.Response = new ApiResponseLog
            {
                StatusCode = response.StatusCode,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength,
                Body = responseBody
            };

            // Capture response headers if enabled
            if (configuration.LogResponseHeaders)
            {
                entry.Response.Headers = CaptureHeaders(response.Headers);
            }
        }

        private static Dictionary<string, string> CaptureHeaders(IHeaderDictionary headers)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in headers)
            {
                // Skip potentially sensitive headers
                if (IsSensitiveHeader(header.Key))
                {
                    result[header.Key] = "***";
                }
                else
                {
                    result[header.Key] = header.Value.ToString();
                }
            }

            return result;
        }

        private static bool IsSensitiveHeader(string headerName)
        {
            return headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                   headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
                   headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ||
                   headerName.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                   headerName.Contains("Key", StringComparison.OrdinalIgnoreCase) ||
                   headerName.Contains("Secret", StringComparison.OrdinalIgnoreCase);
        }
    }
}
