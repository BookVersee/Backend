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
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.RegisterAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "User registered successfully."));
        }

        /// Chức năng: Xác thực đăng nhập tài khoản. Trả về: Chuỗi JWT Access Token và Refresh Token.
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.LoginAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Login successful."));
        }

        /// Chức năng: Đăng nhập nhanh bằng Google (Google Cloud Client ID). Trả về: Chuỗi JWT Access Token và Refresh Token.
        [HttpPost("GoogleLogin")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var response = await _sessionService.GoogleLoginAsync(request, ipAddress, deviceInfo);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Google authentication successful."));
        }

        /// Chức năng: Yêu cầu gửi mã OTP Đặt lại mật khẩu về Email. Trả về: Thông báo đã gửi mã OTP.
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            await _sessionService.SendPasswordResetOtpAsync(request.Email);
            return Ok(ApiResponse<string>.SuccessResponse("Mã OTP đặt lại mật khẩu đã được gửi về Email của bạn. Vui lòng kiểm tra hộp thư."));
        }

        /// Chức năng: Kiểm tra mã OTP hợp lệ trước khi đổi mật khẩu. Trả về: Thông báo OTP hợp lệ.
        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp(VerifyResetOtpRequest request)
        {
            await _sessionService.VerifyResetOtpAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Mã OTP xác thực thành công. Vui lòng nhập mật khẩu mới."));
        }

        /// Chức năng: Xác thực mã OTP và đặt lại mật khẩu mới. Trả về: Thông báo đổi mật khẩu thành công.
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPasswordWithOtpRequest request)
        {
            await _sessionService.ResetPasswordWithOtpAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Đặt lại mật khẩu mới thành công. Vui lòng sử dụng mật khẩu mới để đăng nhập."));
        }

        /// Chức năng: Cấp mới Access Token bằng Refresh Token. Trả về: Chuỗi Access Token mới.
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
        {
            var response = await _sessionService.ValidateAndRefreshTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<TokenResponse>.SuccessResponse(response, "Token refreshed successfully."));
        }
    }
}
