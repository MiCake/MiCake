using MiCake.AspNetCore.ApiLogging;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Controllers
{
    /// <summary>
    /// Controller demonstrating API Logging features.
    /// <para>
    /// This controller showcases the various API logging attributes and configurations
    /// available in MiCake's API Logging feature.
    /// </para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApiLoggingDemoController : ControllerBase
    {
        /// <summary>
        /// Standard endpoint - logged according to global configuration.
        /// </summary>
        /// <remarks>
        /// This endpoint demonstrates normal API logging behavior.
        /// The request and response will be logged based on ApiLoggingOptions.
        /// </remarks>
        [HttpGet("normal")]
        public IActionResult GetNormal()
        {
            return Ok(new
            {
                Message = "This request is logged normally",
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Endpoint with [SkipApiLogging] - not logged at all.
        /// </summary>
        /// <remarks>
        /// Use this attribute for:
        /// - High-frequency endpoints (polling, health checks)
        /// - File downloads (large binary responses)
        /// - Endpoints with sensitive data that shouldn't be logged
        /// </remarks>
        [HttpGet("skip-logging")]
        [SkipApiLogging]
        public IActionResult GetSkipLogging()
        {
            return Ok(new
            {
                Message = "This request is NOT logged",
                Reason = "SkipApiLogging attribute is applied"
            });
        }

        /// <summary>
        /// Endpoint with [AlwaysLog] - always logged regardless of ExcludeStatusCodes.
        /// </summary>
        /// <remarks>
        /// Use this attribute for:
        /// - Critical business operations (payments, orders)
        /// - Security-sensitive operations (login, password changes)
        /// - Audit-required operations
        /// </remarks>
        [HttpPost("always-log")]
        [AlwaysLog]
        public IActionResult PostAlwaysLog([FromBody] DemoRequest request)
        {
            return Ok(new
            {
                Message = "This request is ALWAYS logged",
                Reason = "AlwaysLog attribute is applied",
                ReceivedData = request.Data
            });
        }

        /// <summary>
        /// Endpoint with [LogFullResponse] - logs complete response body.
        /// </summary>
        /// <remarks>
        /// Use this attribute when you need to capture:
        /// - Complete debug information
        /// - Full response for compliance requirements
        /// - Large responses that would normally be truncated
        /// </remarks>
        [HttpGet("full-response")]
        [LogFullResponse]
        public IActionResult GetFullResponse()
        {
            // Generate a larger response that would normally be truncated
            var items = new object[50];
            for (int i = 0; i < 50; i++)
            {
                items[i] = new { Id = i + 1, Name = $"Item {i + 1}", Description = $"Description for item {i + 1}" };
            }

            return Ok(new
            {
                Message = "This entire response is logged without truncation",
                Items = items
            });
        }

        /// <summary>
        /// Endpoint with [LogFullResponse(MaxSize = ...)] - logs response up to custom limit.
        /// </summary>
        [HttpGet("full-response-limited")]
        [LogFullResponse(MaxSize = 2048)]
        public IActionResult GetFullResponseLimited()
        {
            var items = new object[100];
            for (int i = 0; i < 100; i++)
            {
                items[i] = new { Id = i + 1, Value = new string('x', 50) };
            }

            return Ok(new
            {
                Message = "Response logged up to 2KB",
                Items = items
            });
        }

        /// <summary>
        /// Demonstrates sensitive data masking in request/response.
        /// </summary>
        /// <remarks>
        /// Fields configured in ApiLoggingOptions.SensitiveFields will be masked.
        /// Default sensitive fields: password, token, secret, key, authorization
        /// Custom sensitive fields in this app: email, phone
        /// </remarks>
        [HttpPost("sensitive-data")]
        public IActionResult PostSensitiveData([FromBody] SensitiveDataRequest request)
        {
            return Ok(new
            {
                Message = "Sensitive fields in request/response are masked in logs",
                Username = request.Username,
                Email = request.Email,           // Will be masked: "***"
                Password = "hidden-anyway",      // Will be masked: "***"
                Token = "jwt-token-here"         // Will be masked: "***"
            });
        }

        /// <summary>
        /// Demonstrates async operation logging with timing.
        /// </summary>
        [HttpGet("async-operation")]
        public async Task<IActionResult> GetAsyncOperation()
        {
            // Simulate async operation
            await Task.Delay(100);

            return Ok(new
            {
                Message = "Async operation completed",
                Note = "Check ElapsedMilliseconds in the log entry"
            });
        }

        /// <summary>
        /// Demonstrates error response logging.
        /// </summary>
        [HttpGet("error")]
        public IActionResult GetError()
        {
            return BadRequest(new
            {
                Error = "Validation failed",
                Details = new[] { "Field 'name' is required", "Field 'email' must be valid" }
            });
        }

        /// <summary>
        /// Demonstrates server error logging.
        /// </summary>
        [HttpGet("server-error")]
        [AlwaysLog]
        public IActionResult GetServerError()
        {
            throw new InvalidOperationException("Simulated server error for logging demonstration");
        }
    }

    /// <summary>
    /// Demo request model.
    /// </summary>
    public class DemoRequest
    {
        /// <summary>
        /// Sample data field.
        /// </summary>
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model with sensitive fields.
    /// </summary>
    public class SensitiveDataRequest
    {
        /// <summary>
        /// Username (not masked).
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address (masked in logs).
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Password (masked in logs).
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Phone number (masked in logs).
        /// </summary>
        public string Phone { get; set; } = string.Empty;
    }
}
