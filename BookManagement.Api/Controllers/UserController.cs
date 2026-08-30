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

        /// Chức năng: Xem thông tin tài khoản cá nhân. Trả về: Dữ liệu hồ sơ người dùng.
        [Authorize]
        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.GetUserId();
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(profile));
        }

        /// Chức năng: Cập nhật thông tin cá nhân người dùng. Trả về: Dữ liệu hồ sơ mới nhất.
        [Authorize]
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = User.GetUserId();
            var updated = await _userService.UpdateProfileAsync(userId, request);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(updated, "Profile updated successfully."));
        }

        /// TH1 - Bước 1: Gửi mã OTP xác thực đổi/khôi phục mật khẩu qua Gmail.
        [Authorize]
        [HttpPost("SendPasswordOtp")]
        public async Task<IActionResult> SendPasswordOtp(SendOtpRequest request)
        {
            await _userService.SendPasswordOtpAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Mã OTP đã được gửi về Gmail của bạn. Vui lòng kiểm tra hộp thư."));
        }

        /// TH1 - Bước 2: Xác thực mã OTP trước khi nhập mật khẩu mới (Nếu đúng mới cho đổi, sai thì báo lỗi).
        [Authorize]
        [HttpPost("VerifyPasswordOtp")]
        public async Task<IActionResult> VerifyPasswordOtp(VerifyPasswordOtpRequest request)
        {
            await _userService.VerifyPasswordOtpAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Xác thực OTP thành công! Vui lòng chuyển sang bước nhập mật khẩu mới."));
        }

        /// TH1 - Bước 3: Đặt mật khẩu mới (Chỉ cần Email và Mật khẩu mới, không cần ghi lại OTP).
        [Authorize]
        [HttpPost("ResetNewPassword")]
        public async Task<IActionResult> ResetNewPassword(ResetNewPasswordRequest request)
        {
            await _userService.ResetNewPasswordAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Đặt mật khẩu mới thành công!"));
        }

        /// TH2: Thay đổi mật khẩu khi đã đăng nhập (Bằng Mật khẩu cũ + Mật khẩu mới).
        [Authorize]
        [HttpPut("ChangePassword")]
        public async Task<IActionResult> ChangePassword(ChangePasswordWithOldPasswordRequest request)
        {
            var userId = User.GetUserId();
            await _userService.ChangePasswordAsync(userId, request);
            return Ok(ApiResponse<string>.SuccessResponse("Đổi mật khẩu thành công!"));
        }

        /// Chức năng: Truy vấn lịch sử giao dịch tài chính. Trả về: Danh sách biến động số dư tài khoản.
        [Authorize]
        [HttpGet("GetTransactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var userId = User.GetUserId();
            var transactions = await _userService.GetUserTransactionsAsync(userId);
            return Ok(ApiResponse<IEnumerable<TransactionResponse>>.SuccessResponse(transactions));
        }

        /// Chức năng: Đăng ký mở Cửa hàng bán sách mới. Trả về: Thông tin cửa hàng vừa đăng ký ở trạng thái PENDING.
        [Authorize]
        [HttpPost("RegisterShop")]
        public async Task<IActionResult> RegisterShop(RegisterShopRequest request)
        {
            var userId = User.GetUserId();
            var shop = await _userService.RegisterShopAsync(userId, request);
            return Ok(ApiResponse<BookManagement.Service.Shop.ShopResponse>.SuccessResponse(shop, "Shop registration submitted for Admin review."));
        }

        /// Chức năng: Đăng xuất khỏi tài khoản trên thiết bị hiện tại.
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

        /// Chức năng: Đăng xuất khỏi tất cả các thiết bị khác.
        [Authorize]
        [HttpPost("RevokeAllSessions")]
        public async Task<IActionResult> RevokeAllSessions()
        {
            var userId = User.GetUserId();
            await _sessionService.RevokeAllUserSessionsAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("Đã đăng xuất khỏi tất cả các thiết bị thành công."));
        }

        /// Chức năng: Xem danh sách tất cả các phiên / thiết bị đang đăng nhập vào tài khoản.
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
