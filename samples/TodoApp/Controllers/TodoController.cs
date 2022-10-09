using MiCake.AspNetCore.DataWrapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Domain.Aggregates.Todo;
using TodoApp.Domain.Repositories.Todo;
using TodoApp.DtoModels;
using TodoApp.QueryReader.Queries;

namespace TodoApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class TodoController : TodoControllerBase
    {
        private readonly ILogger<TodoController> _logger;
        private readonly ITodoItemRepository _repo;
        private readonly ITodoItemQuery _query;

        public TodoController(ITodoItemRepository repo, ITodoItemQuery query, ILogger<TodoController> logger, ControllerInfrastructure infrastructure) : base(infrastructure)
        {
            _repo = repo;
            _query = query;
            _logger = logger;
        }

        [HttpPost("")]
        public async Task<IActionResult> Create([FromBody] CreateTodoItemDto item)
        {
            if (CurrentUserId == null)
            {
                return Unauthorized();
            }

            var result = TodoItem.Create(item.Title!, item.Detail, CurrentUserId.Value);
            await _repo.AddAsync(result);

            return Ok(result);
        }

        [HttpGet("My/Waiting")]
        [ProducesResponseType(200, Type = typeof(ApiResponse<PagingQueryResult<TodoItem>>))]
        public async Task<IActionResult> PagingGetMyWaitingTodo(int pageIndex, int pageSize)
        {
            var data = await _repo.PagingQueryAsync(new PaginationFilter(pageIndex, pageSize));

            return Ok(data);
        }

        [HttpGet("/")]
        [ProducesResponseType(200, Type = typeof(ApiResponse<PagingQueryResult<TodoItemDto>>))]
        public async Task<IActionResult> PagingQueryAllItems(int pageIndex, int pageSize)
        {
            var result = await _query.PaginationGetTodo(new PaginationFilter(pageIndex, pageSize));
            return Ok(result);
        }

        [HttpPut("{todoId:int}")]
        [ProducesResponseType(200, Type = typeof(ApiResponse<bool>))]
        public async Task<bool> ChangeTodo(int todoId, [FromBody] TodoItemDto item)
        {
            var todoItem = await _repo.FindAsync(todoId);
            if (todoItem is null)
            {
                return false;
            }

            todoItem.ChangeDetail(item.Detail);
            todoItem.ChangeTitle(item.Title!);

            return true;
        }
    }
}