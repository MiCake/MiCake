using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.ApiLogging.Internals
{
    /// <summary>
    /// Processor that truncates large request/response bodies.
    /// </summary>
    internal sealed class TruncationProcessor : IApiLogProcessor
    {
        // Reserve space for the truncation marker "...[truncated]"
        private const int TruncationMarkerReservedBytes = 20;

        public int Order => 10; // Run after masking

        public Task<ApiLogEntry?> ProcessAsync(
            ApiLogEntry entry,
            ApiLogProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            var config = context.Configuration;

            // Process request body truncation
            if (!string.IsNullOrEmpty(entry.Request.Body))
            {
                var requestBytes = Encoding.UTF8.GetByteCount(entry.Request.Body);
                if (requestBytes > config.MaxRequestBodySize)
                {
                    entry.Request.Body = TruncateContent(
                        entry.Request.Body,
                        config.MaxRequestBodySize,
                        config.TruncationStrategy);
                }
            }

            // Process response body truncation
            if (!string.IsNullOrEmpty(entry.Response.Body))
            {
                var responseBytes = Encoding.UTF8.GetByteCount(entry.Response.Body);
                if (responseBytes > config.MaxResponseBodySize)
                {
                    var originalSize = responseBytes;
                    var summary = ExtractJsonSummary(entry.Response.Body);

                    entry.Response.Body = TruncateContent(
                        entry.Response.Body,
                        config.MaxResponseBodySize,
                        config.TruncationStrategy);

                    entry.Response.IsTruncated = true;
                    entry.Response.OriginalSize = originalSize;

                    if (config.TruncationStrategy == TruncationStrategy.TruncateWithSummary &&
                        !string.IsNullOrEmpty(summary))
                    {
                        entry.Response.TruncationSummary = summary;
                    }
                }
            }

            return Task.FromResult<ApiLogEntry?>(entry);
        }

        private static string TruncateContent(string content, int maxSize, TruncationStrategy strategy)
        {
            return strategy switch
            {
                TruncationStrategy.SimpleTruncate => SimpleTruncate(content, maxSize),
                TruncationStrategy.TruncateWithSummary => SimpleTruncate(content, maxSize),
                TruncationStrategy.MetadataOnly => $"[Content: {Encoding.UTF8.GetByteCount(content)} bytes]",
                _ => SimpleTruncate(content, maxSize)
            };
        }

        private static string SimpleTruncate(string content, int maxBytes)
        {
            // Calculate approximate character count for the byte limit
            // This is a simplification; actual UTF-8 encoding may vary
            var charCount = 0;
            var byteCount = 0;

            foreach (var c in content)
            {
                var charBytes = Encoding.UTF8.GetByteCount([c]);
                if (byteCount + charBytes > maxBytes - TruncationMarkerReservedBytes)
                {
                    break;
                }
                byteCount += charBytes;
                charCount++;
            }

            return content[..charCount] + "...[truncated]";
        }

        private static string? ExtractJsonSummary(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                return root.ValueKind switch
                {
                    JsonValueKind.Array => $"Array with {root.GetArrayLength()} items",
                    JsonValueKind.Object => ExtractObjectSummary(root),
                    _ => null
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string ExtractObjectSummary(JsonElement element)
        {
            var propertyCount = 0;
            using var enumerator = element.EnumerateObject();

            while (enumerator.MoveNext())
            {
                propertyCount++;
            }

            // Check for common wrapper patterns
            if (element.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    var itemCount = dataElement.GetArrayLength();

                    // Try to find totalCount property
                    if (element.TryGetProperty("totalCount", out var totalElement) &&
                        totalElement.TryGetInt32(out var total))
                    {
                        return $"Array with {itemCount} items, totalCount: {total}";
                    }

                    return $"Array with {itemCount} items";
                }
            }

            return $"Object with {propertyCount} properties";
        }
    }
}
