using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Service.Auth;
using BookManagement.Service.Common;
using BookManagement.Service.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserResponse = BookManagement.Service.User.UserResponse;

namespace BookManagement.Api.Controllers
{
    /// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserSessionService _sessionService;

        public UserController(IUserService userService, IUserSessionService sessionService)
        {
            _userService = userService;
            _sessionService = sessionService;
        }

        /// Chức năng: Xem thông tin hồ sơ tài khoản cá nhân
        [Authorize]
        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.GetUserId();
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(profile));
        }

        /// Chức năng: Cập nhật thông tin cá nhân (Họ tên, SĐT, Địa chỉ)
        [Authorize]
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = User.GetUserId();
            var updated = await _userService.UpdateProfileAsync(userId, request);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(updated, "Profile updated successfully."));
        }

        /// Chức năng: Gửi mã OTP đổi mật khẩu về Gmail (Bước 1)
        [Authorize]
        [HttpPost("SendPasswordOtp")]
        public async Task<IActionResult> SendPasswordOtp(SendOtpRequest request)
        {
            await _userService.SendPasswordOtpAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Mã OTP đã được gửi về Gmail của bạn. Vui lòng kiểm tra hộp thư."));
        }

        /// Chức năng: Xác thực mã OTP đổi mật khẩu (Bước 2)
        [Authorize]
        [HttpPost("VerifyPasswordOtp")]
        public async Task<IActionResult> VerifyPasswordOtp(VerifyPasswordOtpRequest request)
        {
            await _userService.VerifyPasswordOtpAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Xác thực OTP thành công! Vui lòng chuyển sang bước nhập mật khẩu mới."));
        }

        /// Chức năng: Đặt mật khẩu mới sau khi xác thực OTP thành công (Bước 3)
        [Authorize]
        [HttpPost("ResetNewPassword")]
        public async Task<IActionResult> ResetNewPassword(ResetNewPasswordRequest request)
        {
            await _userService.ResetNewPasswordAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Đặt mật khẩu mới thành công!"));
        }

        /// Chức năng: Đổi mật khẩu tài khoản bằng mật khẩu cũ
        [Authorize]
        [HttpPut("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordWithOldPasswordRequest request)
        {
            var userId = User.GetUserId();
            await _userService.ChangePasswordAsync(userId, request);
            return Ok(ApiResponse<string>.SuccessResponse("Đổi mật khẩu thành công!"));
        }

        /// Chức năng: Xem lịch sử giao dịch tài chính cá nhân
        [Authorize]
        [HttpGet("GetTransactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var userId = User.GetUserId();
            var transactions = await _userService.GetUserTransactionsAsync(userId);
            return Ok(ApiResponse<IEnumerable<TransactionResponse>>.SuccessResponse(transactions));
        }

        /// Chức năng: Đăng ký nâng cấp mở Cửa hàng bán sách
        [Authorize]
        [HttpPost("RegisterShop")]
        public async Task<IActionResult> RegisterShop(RegisterShopRequest request)
        {
            var userId = User.GetUserId();
            var shop = await _userService.RegisterShopAsync(userId, request);
            return Ok(ApiResponse<BookManagement.Service.Shop.ShopResponse>.SuccessResponse(shop, "Shop registration submitted for Admin review."));
        }

        /// Chức năng: Đăng xuất khỏi tài khoản trên thiết bị hiện tại
        [Authorize]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout(RevokeTokenRequest? request)
        {
            if (!string.IsNullOrEmpty(request?.RefreshToken))
            {
                await _sessionService.RevokeSessionAsync(request.RefreshToken);
            }

            var userId = User.GetUserId();
            await _sessionService.RevokeAllUserSessionsAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("Đăng xuất thành công."));
        }

        /// Chức năng: Đăng xuất khỏi tất cả các thiết bị khác
        [Authorize]
        [HttpPost("RevokeAllSessions")]
        public async Task<IActionResult> RevokeAllSessions()
        {
            var userId = User.GetUserId();
            await _sessionService.RevokeAllUserSessionsAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("Đã đăng xuất khỏi tất cả các thiết bị thành công."));
        }

        /// Chức năng: Xem danh sách các thiết bị đang đăng nhập tài khoản
        [Authorize]
        [HttpGet("GetActiveSessions")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var userId = User.GetUserId();
            var sessions = await _sessionService.GetUserSessionsAsync(userId);
            return Ok(ApiResponse<IEnumerable<UserSessionResponse>>.SuccessResponse(sessions, "Lấy danh sách thiết bị đang hoạt động thành công."));
        }
    }
}
