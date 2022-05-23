using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TodoApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class TodoController : TodoControllerBase
    {
        private readonly ILogger<TodoController> _logger;

        public TodoController(ILogger<TodoController> logger, ControllerInfrastructure infrastructure) : base(infrastructure)
        {
            _logger = logger;
        }

        [HttpGet("My/Waiting")]
        public IActionResult GetMyWaitingTodo()
        {
            return Ok("haaha");
        }
    }
}