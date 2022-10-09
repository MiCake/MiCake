using MiCake.AspNetCore.DataWrapper;
using MiCake.Identity.Authentication.JwtToken;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Domain.Aggregates.Identity;
using TodoApp.Domain.Repositories.Identity;
using TodoApp.DtoModels;

namespace TodoApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : TodoControllerBase
    {
        private readonly IJwtAuthManager _jwtManager;
        private readonly ITodoUserRepository _repo;

        public UserController(ITodoUserRepository repo, IJwtAuthManager jwtAuthManager, ControllerInfrastructure infrastructure) : base(infrastructure)
        {
            _repo = repo;
            _jwtManager = jwtAuthManager;
        }

        [HttpPost("create")]
        [ProducesResponseType(200, Type = typeof(ApiResponse<bool>))]
        public async Task<IActionResult> CreateNewUser([FromBody] CreateUserDto user)
        {
            var todoUser = TodoUser.Create(user.LoginName, user.Password);
            todoUser.ChangeUserName(user.FirstName, user.LastName);

            await _repo.AddAndReturnAsync(todoUser);

            return Ok(true);
        }

        [HttpPost("login")]
        [ProducesResponseType(200, Type = typeof(ApiResponse<LoginResultDto>))]
        public async Task<IActionResult> Login([FromBody] LoginUserDto user)
        {
            var todoUser = await _repo.GetByLoginName(user.LoginName);
            if (todoUser is null)
                return BadRequest("User not found");

            if (!todoUser.CheckPassword(user.Password))
                return BadRequest("Password is incorrect");

            var jwtToken = await _jwtManager.CreateToken(todoUser);
            return Ok(new LoginResultDto
            {
                RefreshToken = jwtToken.RefreshToken,
                AccessToken = jwtToken.AccessToken!,
                User = Mapper.Map<TodoUserDto>(todoUser)
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshUserToken([FromBody] SimpleDto<string> data)
        {
            var bearerToken = this.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var jwtToken = await _jwtManager.Refresh(data.Data!, bearerToken);
            return Ok(new LoginResultDto
            {
                RefreshToken = jwtToken.RefreshToken,
                AccessToken = jwtToken.AccessToken!
            });
        }
    }
}
