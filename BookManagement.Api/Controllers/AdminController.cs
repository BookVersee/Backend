using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Admin;
using BookManagement.Service.Book;
using BookManagement.Service.Category;
using BookManagement.Service.Common;
using BookManagement.Service.Delivery;
using BookManagement.Service.Order;
using BookManagement.Service.Shop;
using BookManagement.Service.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    /// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra quyền Admin và trả về ApiResponse.
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ICategoryService _categoryService;

        public AdminController(IAdminService adminService, ICategoryService categoryService)
        {
            _adminService = adminService;
            _categoryService = categoryService;
        }

        /// Chức năng: Lọc danh sách người dùng phân trang
        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers(UserFilterRequest filter)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var users = await _adminService.GetUsersAsync(filter);
            return Ok(ApiResponse<PagedResult<UserResponse>>.SuccessResponse(users));
        }

        /// Chức năng: Xem chi tiết lý lịch người dùng
        [HttpGet("GetUserDetail")]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var userDetail = await _adminService.GetUserDetailAsync(id);
            return Ok(ApiResponse<UserDetailResponse>.SuccessResponse(userDetail));
        }

        /// Chức năng: Cập nhật trạng thái tài khoản
        [HttpPut("UpdateUserStatus")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, UpdateUserStatusRequest request)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            await _adminService.UpdateUserStatusAsync(id, request.Status.ToString());
            return Ok(ApiResponse<string>.SuccessResponse($"User status updated to {request.Status}."));
        }

        /// Chức năng: Lấy danh sách khiếu nại hoàn tiền
        [HttpGet("GetDisputes")]
        public async Task<IActionResult> GetDisputes(ReturnRequestStatus? status)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var disputes = await _adminService.GetDisputesAsync(status?.ToString());
            return Ok(ApiResponse<IEnumerable<DisputeResponse>>.SuccessResponse(disputes));
        }

        /// Chức năng: Xem chi tiết 1 khiếu nại hoàn tiền
        [HttpGet("GetDisputeDetail")]
        public async Task<IActionResult> GetDisputeDetail(Guid disputeId)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var dispute = await _adminService.GetDisputeDetailAsync(disputeId);
            return Ok(ApiResponse<DisputeResponse>.SuccessResponse(dispute));
        }

        /// Chức năng: Admin phán quyết chấp nhận hoặc từ chối khiếu nại
        [HttpPost("ResolveDispute")]
        public async Task<IActionResult> ResolveDispute(Guid disputeId, ResolveDisputeRequest request)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            await _adminService.ResolveDisputeAsync(disputeId, request);
            return Ok(ApiResponse<string>.SuccessResponse("Dispute resolved successfully."));
        }

        /// Chức năng: Lấy danh sách toàn bộ đơn hàng phân trang
        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders(int page = 1, int pageSize = 10)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetAllOrdersAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<OrderResponse>>.SuccessResponse(result));
        }

        /// Chức năng: Lọc danh sách đơn hàng theo trạng thái
        [HttpGet("GetOrdersByStatus")]
        public async Task<IActionResult> GetOrdersByStatus(OrderStatus status, int page = 1, int pageSize = 10)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetOrdersByStatusAsync(status.ToString(), page, pageSize);
            return Ok(ApiResponse<PagedResult<OrderResponse>>.SuccessResponse(result));
        }

        /// Chức năng: Xem thông tin chi tiết đơn hàng
        [HttpGet("GetOrderDetail")]
        public async Task<IActionResult> GetOrderDetail(Guid orderId)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var order = await _adminService.GetOrderDetailAsync(orderId);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
        }

        /// Chức năng: Lấy danh sách toàn bộ sách phân trang
        [HttpGet("GetAllBooks")]
        public async Task<IActionResult> GetAllBooks(int page = 1, int pageSize = 10)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetAllBooksAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<BookResponse>>.SuccessResponse(result));
        }

        /// Chức năng: Ẩn sản phẩm sách vi phạm
        [HttpPut("HideBook")]
        public async Task<IActionResult> HideBook(Guid bookId)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            await _adminService.HideBookAsync(bookId);
            return Ok(ApiResponse<string>.SuccessResponse("Book status updated to HIDDEN."));
        }

        /// Chức năng: Lấy danh sách các thể loại sách
        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _categoryService.GetAllCategoriesAsync();
            return Ok(ApiResponse<IEnumerable<CategoryResponse>>.SuccessResponse(result));
        }

        /// Chức năng: Thêm mới 1 thể loại sách
        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory(CreateCategoryRequest request)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _categoryService.CreateCategoryAsync(request);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(result, "Category created successfully."));
        }

        /// Chức năng: Cập nhật thông tin thể loại sách
        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory(Guid categoryId, UpdateCategoryRequest request)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _categoryService.UpdateCategoryAsync(categoryId, request);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(result, "Category updated successfully."));
        }

        /// Chức năng: Xóa thể loại sách
        [HttpDelete("DeleteCategory")]
        public async Task<IActionResult> DeleteCategory(Guid categoryId)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            await _categoryService.DeleteCategoryAsync(categoryId);
            return Ok(ApiResponse<string>.SuccessResponse("Category deleted successfully."));
        }

        /// Chức năng: Lấy danh sách tất cả Cửa hàng
        [HttpGet("GetAllShops")]
        public async Task<IActionResult> GetAllShops(int page = 1, int pageSize = 10)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetAllShopsAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<ShopResponse>>.SuccessResponse(result));
        }

        /// Chức năng: Khóa Cửa hàng vi phạm
        [HttpPut("LockShop")]
        public async Task<IActionResult> LockShop(Guid shopId, LockShopRequest request)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            await _adminService.LockShopAsync(shopId, request);
            return Ok(ApiResponse<string>.SuccessResponse($"Shop status updated to CLOSED."));
        }

        /// Chức năng: Thống kê chỉ số Dashboard Admin
        [HttpGet("GetDashboardStatistics")]
        public async Task<IActionResult> GetDashboardStatistics(string period = "month")
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetDashboardStatisticsAsync(period);
            return Ok(ApiResponse<DashboardStatisticsResponse>.SuccessResponse(result));
        }

        /// Chức năng: Báo cáo doanh thu toàn sàn
        [HttpGet("GetRevenueReport")]
        public async Task<IActionResult> GetRevenueReport(string period = "month")
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetRevenueReportAsync(period);
            return Ok(ApiResponse<RevenueReportResponse>.SuccessResponse(result));
        }

        /// Chức năng: Lấy danh sách Top sách bán chạy nhất
        [HttpGet("GetTopSellingBooks")]
        public async Task<IActionResult> GetTopSellingBooks(int limit = 10)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetTopSellingBooksAsync(limit);
            return Ok(ApiResponse<IEnumerable<TopSellingBooksResponse>>.SuccessResponse(result));
        }

        /// Chức năng: Lấy danh sách vận đơn giao hàng
        [HttpGet("GetDeliveries")]
        public async Task<IActionResult> GetDeliveries(string? status, int page = 1, int pageSize = 10)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetDeliveriesAsync(status, page, pageSize);
            return Ok(ApiResponse<PagedResult<DeliveryResponse>>.SuccessResponse(result));
        }

        /// Chức năng: Xem chi tiết vận đơn giao hàng
        [HttpGet("GetDeliveryDetail")]
        public async Task<IActionResult> GetDeliveryDetail(Guid deliveryId)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetDeliveryDetailAsync(deliveryId);
            return Ok(ApiResponse<DeliveryResponse>.SuccessResponse(result));
        }

        /// Chức năng: Lấy danh sách các phản hồi của Shop bị báo cáo vi phạm
        [HttpGet("GetReportedResponses")]
        public async Task<IActionResult> GetReportedResponses()
        {
            var (adminId, adminRole) = User.GetUserInfo();
            var result = await _adminService.GetReportedResponsesAsync();
            return Ok(ApiResponse<IEnumerable<ReportedResponseDto>>.SuccessResponse(result));
        }

        /// Chức năng: Phán quyết kiểm duyệt gỡ phản hồi vi phạm của Shop
        [HttpPost("ModerateShopResponse")]
        public async Task<IActionResult> ModerateShopResponse(Guid responseId, bool isDelete, string? adminNote)
        {
            var (adminId, adminRole) = User.GetUserInfo();
            await _adminService.ModerateShopResponseAsync(responseId, isDelete, adminNote);
            return Ok(ApiResponse<string>.SuccessResponse("Đã xử lý kiểm duyệt phản hồi thành công."));
        }
    }
}
