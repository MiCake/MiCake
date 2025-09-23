using System.Threading.Tasks;
using BaseMiCakeApplication.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BaseMiCakeApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(IUserRepository userRepository) : ControllerBase
    {
        private readonly IUserRepository _userRepository = userRepository;

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var user = await _userRepository.FindAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string phone, string pwd, string name, int age)
        {
            var user = Domain.Aggregates.User.Create(phone, pwd, name, age);
            await _userRepository.AddAsync(user);

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, string name, int age)
        {
            var user = await _userRepository.FindAsync(id);
            if (user == null)
                return NotFound();

            user.ChangeUserInfo(name, age);
            await _userRepository.UpdateAsync(user);

            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var user = await _userRepository.FindAsync(id);
            if (user == null)
                return NotFound();

            await _userRepository.DeleteAsync(user);

            return Ok();

        }
    }
}
