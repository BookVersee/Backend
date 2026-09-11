using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Common;
using BookManagement.Service.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Phiên làm việc hết hạn hoặc không hợp lệ.");
            }
            return userId;
        }

        /// Chức năng: Xem thông tin Ví điện tử của người dùng / Cửa hàng
        [HttpGet("my-wallet")]
        public async Task<IActionResult> GetMyWallet()
        {
            var userId = GetUserId();
            var wallet = await _walletService.GetOrCreateWalletAsync(userId);
            return Ok(ApiResponse<WalletResponseDto>.SuccessResponse(wallet, "Lấy thông tin ví điện tử thành công."));
        }

        /// Chức năng: Xem các giao dịch tạm giữ (Escrow) dành cho Cửa hàng
        [HttpGet("escrows")]
        [Authorize(Roles = "SHOP,ADMIN,SUPER_ADMIN")]
        public async Task<IActionResult> GetShopEscrows()
        {
            var shopId = GetUserId();
            var escrows = await _walletService.GetShopEscrowsAsync(shopId);
            return Ok(ApiResponse<object>.SuccessResponse(escrows, "Lấy danh sách giao dịch tạm giữ thành công."));
        }

        /// Chức năng: Yêu cầu rút tiền từ Ví điện tử về Ngân hàng
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
        {
            var userId = GetUserId();
            await _walletService.WithdrawAsync(userId, request);
            return Ok(ApiResponse<string>.SuccessResponse("Yêu cầu rút tiền đã được thực hiện thành công."));
        }
    }
}
