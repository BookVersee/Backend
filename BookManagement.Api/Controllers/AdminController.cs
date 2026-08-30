using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var users = await _adminService.GetUsersAsync(filter);
            return Ok(ApiResponse<PagedResult<UserResponse>>.SuccessResponse(users));
        }

        /// Chức năng: Xem chi tiết lý lịch người dùng
        [HttpGet("GetUserDetail")]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {
            var userDetail = await _adminService.GetUserDetailAsync(id);
            return Ok(ApiResponse<UserDetailResponse>.SuccessResponse(userDetail));
        }

        /// Chức năng: Cập nhật trạng thái tài khoản
        [HttpPut("UpdateUserStatus")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, UpdateUserStatusRequest request)
        {
            await _adminService.UpdateUserStatusAsync(id, request.Status.ToString());
            return Ok(ApiResponse<string>.SuccessResponse($"User status updated to {request.Status}."));
        }

        /// Chức năng: Lấy danh sách khiếu nại hoàn tiền
        [HttpGet("GetDisputes")]
        public async Task<IActionResult> GetDisputes(ReturnRequestStatus? status)
        {
            var disputes = await _adminService.GetDisputesAsync(status?.ToString());
            return Ok(ApiResponse<IEnumerable<DisputeResponse>>.SuccessResponse(disputes));
        }

        /// Chức năng: Xem chi tiết nội dung khiếu nại
        [HttpGet("GetDisputeDetail")]
        public async Task<IActionResult> GetDisputeDetail(Guid id)
        {
            var dispute = await _adminService.GetDisputeDetailAsync(id);
            return Ok(ApiResponse<DisputeResponse>.SuccessResponse(dispute));
        }

        /// Chức năng: Phê duyệt hoặc từ chối khiếu nại
        [HttpPost("ResolveDispute")]
        public async Task<IActionResult> ResolveDispute(Guid id, ResolveDisputeRequest request)
        {
            await _adminService.ResolveDisputeAsync(id, request);
            return Ok(ApiResponse<string>.SuccessResponse("Dispute resolved successfully. Resolution note published."));
        }

        /// Chức năng: Giám sát danh sách tất cả đơn hàng
        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders(int page = 1, int pageSize = 10)
        {
            var orders = await _adminService.GetAllOrdersAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<OrderResponse>>.SuccessResponse(orders));
        }

        /// Chức năng: Lọc đơn hàng theo trạng thái
        [HttpGet("GetOrdersByStatus")]
        public async Task<IActionResult> GetOrdersByStatus(string status, int page = 1, int pageSize = 10)
        {
            var orders = await _adminService.GetOrdersByStatusAsync(status, page, pageSize);
            return Ok(ApiResponse<PagedResult<OrderResponse>>.SuccessResponse(orders));
        }

        /// Chức năng: Xem chi tiết toàn diện đơn hàng
        [HttpGet("GetOrderDetail")]
        public async Task<IActionResult> GetOrderDetailAdmin(Guid orderId)
        {
            var order = await _adminService.GetOrderDetailAsync(orderId);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
        }

        /// Chức năng: Quản lý danh sách toàn bộ sản phẩm sách
        [HttpGet("GetAllBooks")]
        public async Task<IActionResult> GetAllBooks(int page = 1, int pageSize = 10)
        {
            var books = await _adminService.GetAllBooksAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<BookResponse>>.SuccessResponse(books));
        }

        /// Chức năng: Lọc danh sách sách theo trạng thái
        [HttpGet("GetBooksByStatus")]
        public async Task<IActionResult> GetBooksByStatus(string status, int page = 1, int pageSize = 10)
        {
            var books = await _adminService.GetBooksByStatusAsync(status, page, pageSize);
            return Ok(ApiResponse<PagedResult<BookResponse>>.SuccessResponse(books));
        }

        /// Chức năng: Ẩn sản phẩm sách vi phạm
        [HttpPut("HideBook")]
        public async Task<IActionResult> HideBook(Guid bookId)
        {
            await _adminService.HideBookAsync(bookId);
            return Ok(ApiResponse<string>.SuccessResponse("Book hidden successfully."));
        }

        /// Chức năng: Quản lý danh sách tất cả các Shop
        [HttpGet("GetAllShops")]
        public async Task<IActionResult> GetAllShops(int page = 1, int pageSize = 10)
        {
            var shops = await _adminService.GetAllShopsAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<ShopResponse>>.SuccessResponse(shops));
        }

        /// Chức năng: Khóa quyền hoạt động Cửa hàng vi phạm
        [HttpPost("LockShop")]
        public async Task<IActionResult> LockShop(Guid shopId, LockShopRequest request)
        {
            await _adminService.LockShopAsync(shopId, request);
            return Ok(ApiResponse<string>.SuccessResponse("Shop locked successfully."));
        }

        /// Chức năng: Thống kê chỉ số hiệu suất toàn hệ thống
        [HttpGet("GetDashboardStatistics")]
        public async Task<IActionResult> GetDashboardStatistics(string period = "month")
        {
            var stats = await _adminService.GetDashboardStatisticsAsync(period);
            return Ok(ApiResponse<DashboardStatisticsResponse>.SuccessResponse(stats));
        }

        /// Chức năng: Thống kê báo cáo doanh thu
        [HttpGet("GetRevenueReport")]
        public async Task<IActionResult> GetRevenueReport(string period = "month")
        {
            var revenue = await _adminService.GetRevenueReportAsync(period);
            return Ok(ApiResponse<RevenueReportResponse>.SuccessResponse(revenue));
        }

        /// Chức năng: Thống kê top sản phẩm sách bán chạy
        [HttpGet("GetTopSellingBooks")]
        public async Task<IActionResult> GetTopSellingBooks(int limit = 10)
        {
            var books = await _adminService.GetTopSellingBooksAsync(limit);
            return Ok(ApiResponse<IEnumerable<TopSellingBooksResponse>>.SuccessResponse(books));
        }

        /// Chức năng: Giám sát danh sách vận đơn giao hàng
        [HttpGet("GetDeliveries")]
        public async Task<IActionResult> GetDeliveries(string? status, int page = 1, int pageSize = 10)
        {
            var deliveries = await _adminService.GetDeliveriesAsync(status, page, pageSize);
            return Ok(ApiResponse<PagedResult<DeliveryResponse>>.SuccessResponse(deliveries));
        }

        /// Chức năng: Xem chi tiết vận đơn giao hàng
        [HttpGet("GetDeliveryDetail")]
        public async Task<IActionResult> GetDeliveryDetail(Guid deliveryId)
        {
            var delivery = await _adminService.GetDeliveryDetailAsync(deliveryId);
            return Ok(ApiResponse<DeliveryResponse>.SuccessResponse(delivery));
        }

        /// Chức năng: Xem tất cả thể loại sách
        [HttpGet("GetAllCategories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(ApiResponse<IEnumerable<CategoryResponse>>.SuccessResponse(categories));
        }

        /// Chức năng: Thêm mới thể loại sách
        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory(CreateCategoryRequest request)
        {
            var category = await _categoryService.CreateCategoryAsync(request);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(category, "Category created successfully."));
        }

        /// Chức năng: Cập nhật thông tin thể loại sách
        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory(Guid id, UpdateCategoryRequest request)
        {
            var category = await _categoryService.UpdateCategoryAsync(id, request);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(category, "Category updated successfully."));
        }

        /// Chức năng: Xóa thể loại sách
        [HttpDelete("DeleteCategory")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            await _categoryService.DeleteCategoryAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Category deleted successfully."));
        }

        /// Chức năng: Xem danh sách phản hồi của Shop bị báo cáo
        [HttpGet("GetReportedResponses")]
        public async Task<IActionResult> GetReportedResponses()
        {
            var reports = await _adminService.GetReportedResponsesAsync();
            return Ok(ApiResponse<IEnumerable<ReportedResponseDto>>.SuccessResponse(reports));
        }

        /// Chức năng: Xử lý phản hồi của Shop bị báo cáo vi phạm
        [HttpPost("ModerateShopResponse")]
        public async Task<IActionResult> ModerateShopResponse(Guid responseId, bool isDelete = true, string? adminNote = null)
        {
            await _adminService.ModerateShopResponseAsync(responseId, isDelete, adminNote);
            return Ok(ApiResponse<string>.SuccessResponse("Đã xử lý phản hồi bị báo cáo thành công.", "Response moderated successfully."));
        }
    }
}
