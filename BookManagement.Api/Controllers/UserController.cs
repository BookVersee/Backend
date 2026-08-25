using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Models;
using BookManagement.Service.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
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

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(profile));
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();
            var updated = await _userService.UpdateProfileAsync(userId, request);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(updated, "Profile updated successfully."));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _userService.ForgotPasswordAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Password reset link sent to your email."));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _userService.ResetPasswordAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Password reset successfully."));
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            await _userService.VerifyEmailAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Email verified successfully."));
        }

        [Authorize]
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var userId = GetCurrentUserId();
            var transactions = await _userService.GetUserTransactionsAsync(userId);
            return Ok(ApiResponse<object>.SuccessResponse(transactions));
        }

        [Authorize]
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = GetCurrentUserId();
            var notifications = await _userService.GetUserNotificationsAsync(userId);
            return Ok(ApiResponse<object>.SuccessResponse(notifications));
        }

        [Authorize]
        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> ReadNotification(Guid id)
        {
            var userId = GetCurrentUserId();
            await _userService.MarkNotificationAsReadAsync(userId, id);
            return Ok(ApiResponse<string>.SuccessResponse("Notification marked as read."));
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid authentication claims.");
            }
            return userId;
        }
    }
}
