using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public sealed class ProfileController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IFileResourceService _fileResourceService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IUserService userService,
            IFileResourceService fileResourceService,
            ILogger<ProfileController> logger)
        {
            _userService = userService;
            _fileResourceService = fileResourceService;
            _logger = logger;
        }

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetProfileAsync(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        "User id is required."));
            }

            var user =
                await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound(
                    new ProfileMessageResponse(
                        "User was not found."));
            }

            return Ok(user);
        }

        [HttpGet("files/{fileResourceId:guid}")]
        public async Task<IActionResult> GetFileAsync(
            Guid fileResourceId)
        {
            if (fileResourceId == Guid.Empty)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        "File resource id is required."));
            }

            var file =
                await _fileResourceService
                    .GetFileResourceByIdAsync(
                        fileResourceId);

            if (file == null ||
                file.DeletedAtUtc != null)
            {
                return NotFound(
                    new ProfileMessageResponse(
                        "File resource was not found."));
            }

            return Ok(file);
        }
    }
}
