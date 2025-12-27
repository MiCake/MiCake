using MiCake.AspNetCore.ApiLogging;
using Microsoft.AspNetCore.Mvc;

namespace BaseMiCakeApplication.Controllers
{
    /// <summary>
    /// Health check controller with API logging disabled.
    /// <para>
    /// This controller demonstrates using [SkipApiLogging] at the controller level
    /// to disable logging for all actions in the controller.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Health check endpoints are typically called frequently by load balancers,
    /// monitoring systems, and orchestrators. Logging these requests would:
    /// 1. Generate excessive log volume
    /// 2. Increase storage costs
    /// 3. Make it harder to find meaningful logs
    /// 
    /// Use [SkipApiLogging] at the controller level for such scenarios.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [SkipApiLogging]  // All actions in this controller will not be logged
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Basic health check endpoint.
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "BaseMiCakeApplication"
            });
        }

        /// <summary>
        /// Liveness probe for Kubernetes.
        /// </summary>
        [HttpGet("live")]
        public IActionResult GetLiveness()
        {
            return Ok(new { Status = "Live" });
        }

        /// <summary>
        /// Readiness probe for Kubernetes.
        /// </summary>
        [HttpGet("ready")]
        public IActionResult GetReadiness()
        {
            // In a real application, check database connectivity, external services, etc.
            return Ok(new { Status = "Ready" });
        }
    }
}
