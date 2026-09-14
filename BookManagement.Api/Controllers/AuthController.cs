using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Auth;
using BookManagement.Service.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    /// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserSessionService _sessionService;

        public AuthController(IUserSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// Chức năng: Đăng ký tài khoản người dùng mới
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.RegisterAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "User registered successfully."));
        }

        /// Chức năng: Đăng nhập tài khoản hệ thống
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.LoginAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Login successful."));
        }

        /// Chức năng: Đăng nhập bằng tài khoản Google OAuth2
        [HttpPost("GoogleLogin")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.GoogleLoginAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Google authentication successful."));
        }

        /// Chức năng: Gửi mã OTP quên mật khẩu về Email
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            await _sessionService.SendPasswordResetOtpAsync(request.Email);
            return Ok(ApiResponse<string>.SuccessResponse("Mã OTP đặt lại mật khẩu đã được gửi về Email của bạn. Vui lòng kiểm tra hộp thư."));
        }

        /// Chức năng: Xác thực mã OTP quên mật khẩu
        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp(VerifyResetOtpRequest request)
        {
            await _sessionService.VerifyResetOtpAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Mã OTP xác thực thành công. Vui lòng nhập mật khẩu mới."));
        }

        /// Chức năng: Đặt lại mật khẩu mới sau khi xác thực OTP
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordWithOtpRequest request)
        {
            await _sessionService.ResetPasswordWithOtpAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Đặt lại mật khẩu mới thành công. Vui lòng sử dụng mật khẩu mới để đăng nhập."));
        }

        /// Chức năng: Cấp AccessToken mới từ RefreshToken
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
        {
            var response = await _sessionService.ValidateAndRefreshTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Token refreshed successfully."));
        }
    }
}
