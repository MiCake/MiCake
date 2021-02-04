using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Dto;
using MiCake.AspNetCore.Security;
using MiCake.DDD.Domain;
using MiCake.Identity.Authentication.Jwt;
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
        private readonly IJwtAuthManager _jwtManager;
        private IHttpContextAccessor _httpContextAccessor;
        private readonly IRepository<User, Guid> _userRepo;


        public LoginController(
            IJwtAuthManager jwtSupporter,
            IHttpContextAccessor httpContextAccessor,
            IRepository<User, Guid> userRepository)
        {
            _jwtManager = jwtSupporter;
            _httpContextAccessor = httpContextAccessor;
            _userRepo = userRepository;
        }

        [HttpPost]
        public async Task<LoginResultDto> Register(RegisterUserDto registerInfo)
        {
            var user = MiCakeApp.User.Create(registerInfo.Phone, registerInfo.Password, registerInfo.Name, registerInfo.Age);
            await _userRepo.AddAsync(user);

            var token = await _jwtManager.CreateToken(user);

            return new LoginResultDto() { AccessToken = token.AccessToken, HasUser = true, UserInfo = null };
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
        public List<string> GetInfoWithDto([CurrentUser][FromBody] GetUserInfoDto userID)
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
