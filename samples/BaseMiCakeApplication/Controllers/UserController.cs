using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using BaseMiCakeApplication.Dto;
using MiCake.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BaseMiCakeApplication.Controllers
{
    /// <summary>
    /// API controller for user management operations.
    /// </summary>
    /// <remarks>
    /// This controller demonstrates:
    /// 1. Dependency injection with constructor parameters
    /// 2. CRUD operations on aggregate roots
    /// 3. Soft deletion support (via ISoftDeletable interface)
    /// 4. Proper async/await patterns
    /// 5. RESTful API design
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserController> _logger;

        /// <summary>
        /// Initializes a new instance of the UserController.
        /// </summary>
        /// <param name="userRepository">The user repository</param>
        /// <param name="logger">The logger</param>
        public UserController(IUserRepository userRepository, ILogger<UserController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        /// <param name="id">The user ID</param>
        /// <returns>The user if found; otherwise a 404 response</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(long id)
        {
            _logger.LogInformation($"Getting user with ID: {id}");
            var user = await _userRepository.FindAsync(id);
            
            if (user == null)
                return NotFound("User not found");

            return Ok(user);
        }

        /// <summary>
        /// Creates a new user with the provided credentials.
        /// </summary>
        /// <param name="createDto">The user creation data</param>
        /// <returns>The created user</returns>
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] RegisterUserDto createDto)
        {
            _logger.LogInformation($"Creating new user with phone: {createDto.Phone}");

            try
            {
                // Use factory method for creation
                var user = Domain.Aggregates.User.Create(
                    phone: createDto.Phone,
                    pwd: createDto.Password,
                    name: createDto.Name,
                    email: createDto.Email,
                    age: createDto.Age ?? 0);

                await _userRepository.AddAsync(user);

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning($"User creation failed: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates an existing user's information.
        /// </summary>
        /// <param name="id">The user ID</param>
        /// <param name="updateDto">The update data</param>
        /// <returns>The updated user</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(long id, [FromBody] UpdateUserDto updateDto)
        {
            _logger.LogInformation($"Updating user with ID: {id}");

            var user = await _userRepository.FindAsync(id);
            if (user == null)
                return NotFound("User not found");

            try
            {
                // Update user properties
                if (!string.IsNullOrEmpty(updateDto.Name) || updateDto.Age.HasValue)
                    user.ChangeUserInfo(updateDto.Name ?? user.Name, updateDto.Age ?? user.Age);

                if (!string.IsNullOrEmpty(updateDto.Phone))
                    user.ChangePhone(updateDto.Phone);

                if (!string.IsNullOrEmpty(updateDto.Email))
                    user.UpdateEmail(updateDto.Email);

                if (!string.IsNullOrEmpty(updateDto.Avatar))
                    user.SetAvatar(updateDto.Avatar);

                await _userRepository.UpdateAsync(user);

                return Ok(user);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning($"User update failed: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Soft-deletes a user by ID.
        /// </summary>
        /// <remarks>
        /// This method performs a soft delete, marking the user as deleted
        /// without removing them from the database. The DbContext has a query filter
        /// to exclude soft-deleted users by default.
        /// </remarks>
        /// <param name="id">The user ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            _logger.LogInformation($"Deleting user with ID: {id}");

            var user = await _userRepository.FindAsync(id);
            if (user == null)
                return NotFound("User not found");

            // Mark as deleted (soft delete via ISoftDeletable)
            user.IsDeleted = true;
            await _userRepository.UpdateAsync(user);

            return Ok("User deleted successfully");
        }
    }
}
