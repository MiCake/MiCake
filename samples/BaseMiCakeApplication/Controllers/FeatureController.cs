using System.Collections.Generic;
using MiCake.Core;
using Microsoft.AspNetCore.Mvc;

namespace BaseMiCakeApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeatureController : ControllerBase
    {
        /// <summary>
        /// Demonstrates different response types and exception handling.
        /// </summary>
        [HttpGet("not-found")]
        public IActionResult DemoNotFound() => NotFound("This resource was not found");

        /// <summary>
        /// Demonstrates MiCakeException (HTTP 500).
        /// </summary>
        [HttpGet("exception/handle")]
        public IActionResult DemoMiCakeException()
            => throw new MiCakeException("This is a critical error");

        /// <summary>
        /// Demonstrates successful string response.
        /// </summary>
        [HttpGet("result-type/string")]
        public IActionResult DemoStringResult() => Ok("MiCake Framework");

        /// <summary>
        /// Demonstrates successful list response.
        /// </summary>
        [HttpGet("result-type/list")]
        public IActionResult DemoListResult() => Ok(new List<int> { 1, 2, 3, 4, 5 });

        /// <summary>
        /// Demonstrates SlightMiCakeException (HTTP 200 with error message).
        /// </summary>
        [HttpGet("exception/slight")]
        public IActionResult DemoSlightMiCakeException()
            => throw new SlightMiCakeException("This is a soft error handled gracefully");
    }
}
