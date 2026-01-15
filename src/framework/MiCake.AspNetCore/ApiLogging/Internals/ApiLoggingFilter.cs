using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace MiCake.AspNetCore.ApiLogging.Internals
{
    /// <summary>
    /// MVC Resource and Result Filter that logs API requests and responses.
    /// </summary>
    internal sealed class ApiLoggingFilter : IAsyncResourceFilter, IAsyncResultFilter, IOrderedFilter
    {
        private readonly IApiLoggingConfigProvider _configProvider;
        private readonly IApiLogEntryFactory _entryFactory;
        private readonly IEnumerable<IApiLogProcessor> _processors;
        private readonly IApiLogWriter _logWriter;
        private readonly ILogger<ApiLoggingFilter> _logger;

        private const string LogEntryKey = "MiCake.ApiLogging.Entry";
        private const string StopwatchKey = "MiCake.ApiLogging.Stopwatch";
        private const string ConfigKey = "MiCake.ApiLogging.Config";
        private const string AttributeInfoKey = "MiCake.ApiLogging.AttributeInfo";

        // Cache compiled regex patterns for glob matching to avoid repeated compilation
        private static readonly ConcurrentDictionary<string, Regex> _globPatternCache = new();

        public int Order => int.MaxValue; // Run last

        public ApiLoggingFilter(
            IApiLoggingConfigProvider configProvider,
            IApiLogEntryFactory entryFactory,
            IEnumerable<IApiLogProcessor> processors,
            IApiLogWriter logWriter,
            ILogger<ApiLoggingFilter> logger)
        {
            _configProvider = configProvider;
            _entryFactory = entryFactory;
            _processors = processors.OrderBy(p => p.Order).ToList();
            _logWriter = logWriter;
            _logger = logger;
        }

        /// <summary>
        /// Resource filter runs before model binding, allowing us to enable request buffering
        /// before the [FromBody] model binder reads the request stream.
        /// </summary>
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var config = await _configProvider.GetEffectiveConfigAsync().ConfigureAwait(false);

            // Check if logging is enabled globally
            if (!config.Enabled)
            {
                await next().ConfigureAwait(false);
                return;
            }

            // Get attribute info for the action
            ApiLoggingAttributeInfo? attributeInfo = null;
            if (context.ActionDescriptor is ControllerActionDescriptor controllerAction)
            {
                attributeInfo = ApiLoggingAttributeCache.GetInfo(controllerAction);

                // Check if logging should be skipped for this action
                if (attributeInfo.SkipLogging)
                {
                    await next().ConfigureAwait(false);
                    return;
                }
            }

            // Check if path is excluded
            var path = context.HttpContext.Request.Path.Value ?? string.Empty;
            if (IsPathExcluded(path, config.ExcludedPaths))
            {
                await next().ConfigureAwait(false);
                return;
            }

            // Store config and attribute info for result filter
            context.HttpContext.Items[ConfigKey] = config;
            context.HttpContext.Items[AttributeInfoKey] = attributeInfo;

            // Start timing
            var stopwatch = Stopwatch.StartNew();
            context.HttpContext.Items[StopwatchKey] = stopwatch;

            // Create initial log entry with request info
            var entry = _entryFactory.CreateEntry(context.HttpContext, config);

            // CRITICAL: Enable buffering BEFORE model binding reads the request body
            if (config.LogRequestBody)
            {
                entry.Request.Body = await CaptureRequestBodyAsync(
                    context.HttpContext,
                    config.MaxRequestBodySize).ConfigureAwait(false);
            }

            context.HttpContext.Items[LogEntryKey] = entry;

            await next().ConfigureAwait(false);
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // Execute the result first
            await next().ConfigureAwait(false);

            // Check if we have a log entry to process
            if (!context.HttpContext.Items.TryGetValue(LogEntryKey, out var entryObj) ||
                entryObj is not ApiLogEntry entry)
            {
                return;
            }

            if (!context.HttpContext.Items.TryGetValue(ConfigKey, out var configObj) ||
                configObj is not ApiLoggingEffectiveConfig config)
            {
                return;
            }

            var attributeInfo = context.HttpContext.Items[AttributeInfoKey] as ApiLoggingAttributeInfo;
            var stopwatch = context.HttpContext.Items[StopwatchKey] as Stopwatch;
            stopwatch?.Stop();

            var statusCode = context.HttpContext.Response.StatusCode;

            // Check if this status code should be logged
            if (!ShouldLogStatusCode(statusCode, config, attributeInfo))
            {
                return;
            }

            // Capture response body
            string? responseBody = null;
            if (config.LogResponseBody)
            {
                responseBody = await CaptureResponseBodyAsync(
                    context,
                    config,
                    attributeInfo).ConfigureAwait(false);
            }

            // Populate response information
            _entryFactory.PopulateResponse(
                entry,
                context.HttpContext,
                config,
                responseBody,
                stopwatch?.Elapsed ?? TimeSpan.Zero);

            // Process through pipeline
            var processedEntry = await ProcessEntryAsync(entry, context.HttpContext, config).ConfigureAwait(false);
            if (processedEntry == null)
            {
                return;
            }

            // Write log
            try
            {
                await _logWriter.WriteAsync(processedEntry).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write API log entry for {Path}", entry.Request.Path);
            }
        }

        private static bool ShouldLogStatusCode(
            int statusCode,
            ApiLoggingEffectiveConfig config,
            ApiLoggingAttributeInfo? attributeInfo)
        {
            // AlwaysLog attribute overrides exclusions
            if (attributeInfo?.AlwaysLog == true)
            {
                return true;
            }

            // Check if status code is excluded
            if (config.ExcludeStatusCodes.Count > 0 && config.ExcludeStatusCodes.Contains(statusCode))
            {
                return false;
            }

            return true;
        }

        private static bool IsPathExcluded(string path, List<string> excludedPaths)
        {
            return excludedPaths != null && excludedPaths.Any(pattern => MatchesGlobPattern(path, pattern));
        }

        private static bool MatchesGlobPattern(string path, string pattern)
        {
            // Cache compiled regex patterns to avoid repeated compilation on each request
            var regex = _globPatternCache.GetOrAdd(pattern, p =>
            {
                var regexPattern = "^" + Regex.Escape(p)
                    .Replace("\\*\\*", ".*")
                    .Replace("\\*", "[^/]*") + "$";
                return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            });

            return regex.IsMatch(path);
        }

        private static async Task<string?> CaptureRequestBodyAsync(HttpContext context, int maxSize)
        {
            var request = context.Request;

            // Skip if no body or too large
            if (!request.ContentLength.HasValue || request.ContentLength == 0)
            {
                return null;
            }

            // Enable buffering to allow reading the body multiple times
            request.EnableBuffering();

            var bodySize = Math.Min((int)request.ContentLength.Value, maxSize);
            var buffer = ArrayPool<byte>.Shared.Rent(bodySize);
            try
            {
                request.Body.Position = 0;
                var bytesRead = await request.Body.ReadAsync(buffer.AsMemory(0, bodySize)).ConfigureAwait(false);
                request.Body.Position = 0;

                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            catch (Exception ex)
            {
                // Log warning for unexpected failures during request body capture
                // This helps with debugging while gracefully degrading the logging feature
                if (context.RequestServices.GetService(typeof(ILogger<ApiLoggingFilter>)) is ILogger<ApiLoggingFilter> logger)
                {
                    logger.LogWarning(ex, "Failed to capture request body for API logging");
                }
                return null;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task<string?> CaptureResponseBodyAsync(
            ResultExecutingContext context,
            ApiLoggingEffectiveConfig config,
            ApiLoggingAttributeInfo? attributeInfo)
        {
            // Check content type exclusion
            var contentType = context.HttpContext.Response.ContentType;
            if (!string.IsNullOrEmpty(contentType) && IsContentTypeExcluded(contentType, config.ExcludedContentTypes))
            {
                return $"[Content-Type excluded: {contentType}]";
            }

            // Determine max size based on attribute
            var maxSize = config.MaxResponseBodySize;
            if (attributeInfo?.LogFullResponse == true)
            {
                maxSize = attributeInfo.MaxResponseSize > 0 ? attributeInfo.MaxResponseSize : int.MaxValue;
            }

            // Try to get body from ObjectResult
            if (context.Result is ObjectResult objectResult && objectResult.Value != null)
            {
                string? responseText;
                try
                {
                    responseText = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
                }
                catch
                {
                    responseText = objectResult.Value.ToString();
                }

                // Truncate if exceeds max size
                if (responseText != null && responseText.Length > maxSize)
                {
                    return string.Concat(responseText.AsSpan(0, maxSize), "... [truncated]");
                }

                return responseText;
            }

            return null;
        }

        private static bool IsContentTypeExcluded(string contentType, List<string> excludedContentTypes)
        {
            return excludedContentTypes != null && excludedContentTypes.Any(pattern =>
                pattern.EndsWith("/*")
                    ? contentType.StartsWith(pattern[..^2], StringComparison.OrdinalIgnoreCase)
                    : contentType.Equals(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<ApiLogEntry?> ProcessEntryAsync(
            ApiLogEntry entry,
            HttpContext httpContext,
            ApiLoggingEffectiveConfig config)
        {
            var context = new ApiLogProcessingContext(httpContext, config);
            ApiLogEntry? currentEntry = entry;

            foreach (var processor in _processors)
            {
                if (currentEntry == null)
                {
                    break;
                }

                try
                {
                    currentEntry = await processor.ProcessAsync(currentEntry, context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "API log processor {ProcessorType} failed", processor.GetType().Name);
                }
            }

            return currentEntry;
        }
    }
}
