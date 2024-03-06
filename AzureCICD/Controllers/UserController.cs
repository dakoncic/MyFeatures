using AzureCICD.DTO;
using Core.DomainModels;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AzureCICD.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetAll()
        {
            try
            {
                var users = await _userService.GetAllPostsAsync();

                var userDTOs = users.Select(u => new UserDTO
                {
                    Id = u.Id,
                    Content = u.Content
                }).ToList();

                return Ok(userDTOs);
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<UserDTO>> CreatePost(UserDTO userDTO)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var user = new User
                {
                    Content = userDTO.Content
                };

                await _userService.CreatePostAsync(user);

                var newUserDTO = new UserDTO
                {
                    Id = user.Id,
                    Content = user.Content
                };

                return Ok(newUserDTO);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
