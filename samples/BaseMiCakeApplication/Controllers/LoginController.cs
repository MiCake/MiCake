using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Dto;
using Mapster;
using MiCake.AspNetCore.Security;
using MiCake.DDD.Domain;
using MiCake.Identity.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiCakeApp = BaseMiCakeApplication.Domain.Aggregates;

namespace BaseMiCakeApplication.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class LoginController : ControllerBase
    {
        private readonly IJwtSupporter _jwtSupporter;
        private IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<User, Guid> _userRepo;


        public LoginController(
            IJwtSupporter jwtSupporter,
            IHttpContextAccessor httpContextAccessor,
            IRepository<User, Guid> userRepository)
        {
            _jwtSupporter = jwtSupporter;
            _httpContextAccessor = httpContextAccessor;
            _userRepo = userRepository;
        }

        [HttpPost]
        public async Task<LoginResultDto> Register(RegisterUserDto registerInfo)
        {
            var user = MiCakeApp.User.Create(registerInfo.Phone, registerInfo.Password, registerInfo.Name, registerInfo.Age);
            await _userRepo.AddAsync(user);

            var token = _jwtSupporter.CreateToken(user);

            return new LoginResultDto() { AccessToken = token, HasUser = true, UserInfo = user.Adapt<UserDto>() };
        }

        [HttpGet]
        [Authorize]
        public List<string> GetInfo([CurrentUser] Guid userID)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userInfo = httpContext?.User;

            var result = userInfo?.Claims.Select(s => s.Value).ToList();
            return result;
        }

        [HttpPost]
        [Authorize]
        public List<string> GetInfoWithDto([CurrentUser] [FromBody] GetUserInfoDto userID)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userInfo = httpContext?.User;

            var result = userInfo?.Claims.Select(s => s.Value).ToList();
            return result;
        }
    }

    public class GetUserInfoDto
    {
        [VerifyUserId]
        public Guid UserID { get; set; }

        public string UserName { get; set; }
    }
}
