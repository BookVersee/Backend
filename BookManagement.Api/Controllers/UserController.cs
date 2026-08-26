using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Models;
using BookManagement.Service.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// Chức năng: Xem thông tin tài khoản cá nhân. Trả về: Dữ liệu hồ sơ người dùng.
        [Authorize]
        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(profile));
        }

        /// Chức năng: Cập nhật thông tin cá nhân người dùng. Trả về: Dữ liệu hồ sơ mới nhất.
        [Authorize]
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();
            var updated = await _userService.UpdateProfileAsync(userId, request);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(updated, "Profile updated successfully."));
        }

        /// Chức năng: Gửi liên kết khôi phục mật khẩu qua email. Trả về: Thông báo đã gửi email khôi phục.
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _userService.ForgotPasswordAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Password reset link sent to your email."));
        }

        /// Chức năng: Đặt lại mật khẩu mới qua token xác thực. Trả về: Thông báo đổi mật khẩu thành công.
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            await _userService.ResetPasswordAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Password reset successfully."));
        }

        /// Chức năng: Xác thực địa chỉ email tài khoản. Trả về: Thông báo xác minh email thành công.
        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            await _userService.VerifyEmailAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse("Email verified successfully."));
        }

        /// Chức năng: Truy vấn lịch sử giao dịch tài chính. Trả về: Danh sách biến động số dư tài khoản.
        [Authorize]
        [HttpGet("GetTransactions")]
        public async Task<IActionResult> GetTransactions()
        {
            var userId = GetCurrentUserId();
            var transactions = await _userService.GetUserTransactionsAsync(userId);
            return Ok(ApiResponse<IEnumerable<TransactionResponse>>.SuccessResponse(transactions));
        }

        /// Chức năng: Đăng ký mở Cửa hàng bán sách mới. Trả về: Thông tin cửa hàng vừa đăng ký ở trạng thái PENDING.
        [Authorize]
        [HttpPost("RegisterShop")]
        public async Task<IActionResult> RegisterShop([FromBody] RegisterShopRequest request)
        {
            var userId = GetCurrentUserId();
            var shop = await _userService.RegisterShopAsync(userId, request);
            return Ok(ApiResponse<BookManagement.Service.Admin.ShopResponse>.SuccessResponse(shop, "Shop registration submitted for Admin review."));
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
