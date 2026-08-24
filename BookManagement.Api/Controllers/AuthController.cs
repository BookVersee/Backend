using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Auth;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserSessionService _sessionService;

        public AuthController(IUserSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.RegisterAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "User registered successfully."));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.LoginAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Login successful."));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _sessionService.ValidateAndRefreshTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Token refreshed successfully."));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RevokeTokenRequest request)
        {
            await _sessionService.RevokeSessionAsync(request.RefreshToken);
            return Ok(ApiResponse<string>.SuccessResponse("Logged out successfully.", "Session revoked."));
        }

        [Authorize]
        [HttpPost("revoke-all")]
        public async Task<IActionResult> RevokeAllSessions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<string>.FailureResponse("Invalid token claims."));
            }

            await _sessionService.RevokeAllUserSessionsAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("All user sessions revoked successfully."));
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<string>.FailureResponse("Invalid token claims."));
            }

            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            return Ok(ApiResponse<object>.SuccessResponse(sessions, "Active sessions retrieved."));
        }
    }
}
