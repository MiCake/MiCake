using System.Threading;
using System.Threading.Tasks;

namespace MiCake.AspNetCore.ApiLogging.Internals
{
    /// <summary>
    /// Processor that masks sensitive data in log entries.
    /// </summary>
    internal sealed class SensitiveMaskProcessor : IApiLogProcessor
    {
        private readonly ISensitiveDataMasker _masker;

        public int Order => 0; // Run first

        public SensitiveMaskProcessor(ISensitiveDataMasker masker)
        {
            _masker = masker;
        }

        public Task<ApiLogEntry?> ProcessAsync(
            ApiLogEntry entry,
            ApiLogProcessingContext context,
            CancellationToken cancellationToken = default)
        {
            var sensitiveFields = context.Configuration.SensitiveFields;

            if (sensitiveFields.Count == 0)
            {
                return Task.FromResult<ApiLogEntry?>(entry);
            }

            // Mask request body
            if (!string.IsNullOrEmpty(entry.Request.Body))
            {
                entry.Request.Body = _masker.Mask(entry.Request.Body, sensitiveFields);
            }

            // Mask response body
            if (!string.IsNullOrEmpty(entry.Response.Body))
            {
                entry.Response.Body = _masker.Mask(entry.Response.Body, sensitiveFields);
            }

            // Mask query string
            if (!string.IsNullOrEmpty(entry.Request.QueryString))
            {
                entry.Request.QueryString = _masker.Mask(entry.Request.QueryString, sensitiveFields);
            }

            return Task.FromResult<ApiLogEntry?>(entry);
        }
    }
}
