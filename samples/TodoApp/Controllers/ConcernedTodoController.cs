using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Domain.Aggregates.Todo;
using TodoApp.Domain.Repositories.Todo;
using TodoApp.DtoModels;
using TodoApp.QueryReader.Queries;

namespace TodoApp.Controllers
{
    [ApiController]
    [Route("Todo/Concerned")]
    [Authorize]
    public class ConcernedTodoController : TodoControllerBase
    {
        private readonly IConcernedTodoRepository _repo;
        private readonly IConcernedTodoQuery _query;

        public ConcernedTodoController(IConcernedTodoRepository repo, IConcernedTodoQuery query, ControllerInfrastructure infrastructure) : base(infrastructure)
        {
            _repo = repo;
            _query = query;
        }

        [HttpPost("")]
        public async Task<IActionResult> MarkATodoToConcerned([FromBody] SimpleDto<int> data)
        {
            if (this.CurrentUserId == null)
            {
                return Unauthorized();
            }

            if (await _repo.GetPersonHasConcernedCuurentTodoItem(this.CurrentUserId.Value, data.Data))
            {
                return BadRequest("You have already marked this todo item as concerned.");
            }

            var item = ConcernedTodo.Create(data.Data, this.CurrentUserId.Value);
            await _repo.AddAsync(item);

            return Ok(item);
        }

        [HttpGet("")]
        [ProducesResponseType(200, Type = typeof(PagingQueryResult<TodoItemDto>))]
        public async Task<IActionResult> QueryMyConcernedTodos(int pageIndex, int pageSize)
        {
            if (this.CurrentUserId == null)
            {
                return Unauthorized();
            }

            var result = await _query.PaginationGetPersonConcernedTodo(new PaginationFilter(pageIndex, pageSize), this.CurrentUserId.Value);
            return Ok(result);
        }
    }
}
