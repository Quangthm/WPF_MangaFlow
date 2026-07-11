using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/profile/password")]
    public sealed class ProfilePasswordController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<ProfilePasswordController> _logger;

        public ProfilePasswordController(
            IUserService userService,
            ILogger<ProfilePasswordController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetAsync(
            [FromBody] ResetProfilePasswordRequest request)
        {
            if (request.UserId == Guid.Empty)
            {
                return BadRequest(
                    new ProfilePasswordResponse(
                        "User id is required."));
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(
                    new ProfilePasswordResponse(
                        "New password cannot be empty."));
            }

            if (request.NewPassword.Length < 8)
            {
                return BadRequest(
                    new ProfilePasswordResponse(
                        "New password must be at least 8 characters."));
            }

            try
            {
                await _userService.ResetPasswordAsync(
                    request.UserId,
                    request.NewPassword);

                return Ok(
                    new ProfilePasswordResponse(
                        "Password reset successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ProfilePasswordResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to reset password for user {UserId}.",
                    request.UserId);

                return Problem(
                    detail:
                        "The password could not be reset. Please try again.",
                    statusCode:
                        StatusCodes.Status500InternalServerError);
            }
        }
    }
}
