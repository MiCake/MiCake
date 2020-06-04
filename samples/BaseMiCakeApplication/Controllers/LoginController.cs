﻿using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.Identity.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace BaseMiCakeApplication.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class LoginController : ControllerBase
    {
        private readonly IJwtSupporter _jwtSupporter;
        private IHttpContextAccessor _httpContextAccessor;
        public LoginController(IJwtSupporter jwtSupporter, IHttpContextAccessor httpContextAccessor)
        {
            _jwtSupporter = jwtSupporter;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        public string Login()
        {
            var user = new User();
            user.SetName("bob");

            var result = _jwtSupporter.CreateToken(user);
         
            var hanlder = new JwtSecurityTokenHandler();
            var reader = hanlder.ReadToken(result);

            return result;
        }

        [HttpGet]
        [Authorize]
        public List<string> GetInfo()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userInfo = httpContext?.User;

            var result = userInfo?.Claims.Select(s => s.Value).ToList();
            return result;
        }
    }
}
