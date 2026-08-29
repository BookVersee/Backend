using System;
using System.Collections.Generic;
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
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserSessionService _sessionService;

        public AuthController(IUserSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// Chức năng: Đăng ký tài khoản người dùng mới. Trả về: Chuỗi JWT Token và thông tin tài khoản.
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.RegisterAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "User registered successfully."));
        }

        /// Chức năng: Xác thực đăng nhập tài khoản. Trả về: Chuỗi JWT Access Token và Refresh Token.
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.LoginAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Login successful."));
        }

        /// Chức năng: Đăng nhập nhanh bằng Google (Google Cloud Client ID). Trả về: Chuỗi JWT Access Token và Refresh Token.
        [HttpPost("GoogleLogin")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.GoogleLoginAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Google authentication successful."));
        }

        /// Chức năng: Yêu cầu gửi mã OTP Đặt lại mật khẩu về Email. Trả về: Thông báo đã gửi mã OTP.
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _sessionService.SendPasswordResetOtpAsync(request.Email);
            return Ok(ApiResponse<string>.SuccessResponse("Mã OTP đặt lại mật khẩu đã được gửi về Email của bạn. Vui lòng kiểm tra hộp thư."));
        }

        /// Chức năng: Xác thực mã OTP và đặt lại mật khẩu mới. Trả về: Thông báo đổi mật khẩu thành công.
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithOtpRequest request)
        {
            await _sessionService.ResetPasswordWithOtpAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Đặt lại mật khẩu mới thành công. Vui lòng sử dụng mật khẩu mới để đăng nhập."));
        }

        /// Chức năng: Cấp mới Access Token bằng Refresh Token. Trả về: Chuỗi Access Token mới.
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _sessionService.ValidateAndRefreshTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Token refreshed successfully."));
        }

        /// Chức năng: Đăng xuất và hủy phiên làm việc hiện tại. Trả về: Thông báo đăng xuất thành công.
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout([FromBody] RevokeTokenRequest? request)
        {
            if (!string.IsNullOrEmpty(request?.RefreshToken))
            {
                await _sessionService.RevokeSessionAsync(request.RefreshToken);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                await _sessionService.RevokeAllUserSessionsAsync(userId);
            }

            return Ok(ApiResponse<string>.SuccessResponse("Logged out successfully.", "Session revoked."));
        }

        /// Chức năng: Đăng xuất khỏi tất cả các thiết bị. Trả về: Thông báo thu hồi toàn bộ phiên đăng nhập.
        [Authorize]
        [HttpPost("RevokeAllSessions")]
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

        /// Chức năng: Xem danh sách các phiên đăng nhập đang hoạt động. Trả về: Danh sách phiên làm việc kích hoạt.
        [Authorize]
        [HttpGet("GetActiveSessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<string>.FailureResponse("Invalid token claims."));
            }

            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            return Ok(ApiResponse<IEnumerable<UserSessionResponse>>.SuccessResponse(sessions, "Active sessions retrieved."));
        }
    }
}
