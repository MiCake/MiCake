using MiCake.AspNetCore.Controller;
using Microsoft.AspNetCore.Mvc;

namespace TodoApp.Controllers
{
    [ApiController]
    [Route("status")]
    public class DifferentStatusController : MiCakeControllerBase
    {
        [HttpGet("nofound")]
        public IActionResult NoFoundResult()
        {
            return NotFound(new { Data = "no found." });
        }

        [HttpGet("badrequest")]
        public IActionResult BadRequestResult()
        {
            return BadRequest(new { Data = "bad request." });
        }

        [HttpGet("wrap/badrequest")]
        public IActionResult WrappedBadRequestResult()
        {
            return BadRequest("00.01", "bad request", new { Data = "bad request." });
        }

        [HttpGet("wrap/string")]
        public string AutoWrappOkData()
        {
            return "I am DJ.";
        }

        [HttpGet("wrap/obj")]
        public object AutoWrappOkObjData()
        {
            return new { Name = "Dj", Title = "I am DJ." };
        }

        [HttpGet("wrap/ok")]
        public IActionResult WrappedOkData()
        {
            return Ok(new { Name = "Dj", Title = "the value is from MiCakeControllerBase" });
        }

        [HttpPost("modelValidation")]
        public IActionResult ModelValidationTest([FromBody] ValidationDto data)
        {
            return Ok(data.GuidData);
        }
    }

    public class ValidationDto
    {
        public Guid GuidData { get; set; }
    }
}
