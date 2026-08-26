using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Admin;
using BookManagement.Service.Category;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [Authorize(Roles = "ADMIN")]
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

        /// Chức năng: Trang quản trị - Lọc danh sách tài khoản người dùng. Trả về: Danh sách người dùng phân trang.
        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterRequest filter)
        {
            var users = await _adminService.GetUsersAsync(filter);
            return Ok(ApiResponse<PagedResult<UserResponse>>.SuccessResponse(users));
        }

        /// Chức năng: Trang quản trị - Xem thông tin chi tiết người dùng. Trả về: Hồ sơ lý lịch và lịch sử số dư.
        [HttpGet("GetUserDetail")]
        public async Task<IActionResult> GetUserDetail([FromQuery] Guid id)
        {
            var userDetail = await _adminService.GetUserDetailAsync(id);
            return Ok(ApiResponse<UserDetailResponse>.SuccessResponse(userDetail));
        }

        /// Chức năng: Trang quản trị - Cập nhật trạng thái tài khoản. Trả về: Thông báo xác nhận cập nhật trạng thái.
        [HttpPut("UpdateUserStatus")]
        public async Task<IActionResult> UpdateUserStatus([FromQuery] Guid id, [FromBody] UpdateUserStatusRequest request)
        {
            await _adminService.UpdateUserStatusAsync(id, request.Status.ToString());
            return Ok(ApiResponse<string>.SuccessResponse($"User status updated to {request.Status}."));
        }

        /// Chức năng: Trang quản trị - Lấy danh sách đơn khiếu nại hoàn tiền. Trả về: Danh sách yêu cầu trả hàng.
        [HttpGet("GetDisputes")]
        public async Task<IActionResult> GetDisputes([FromQuery] ReturnRequestStatus? status)
        {
            var disputes = await _adminService.GetDisputesAsync(status?.ToString());
            return Ok(ApiResponse<IEnumerable<DisputeResponse>>.SuccessResponse(disputes));
        }

        /// Chức năng: Trang quản trị - Xem chi tiết đơn khiếu nại. Trả về: Chi tiết nội dung khiếu nại.
        [HttpGet("GetDisputeDetail")]
        public async Task<IActionResult> GetDisputeDetail([FromQuery] Guid id)
        {
            var dispute = await _adminService.GetDisputeDetailAsync(id);
            return Ok(ApiResponse<DisputeResponse>.SuccessResponse(dispute));
        }

        /// Chức năng: Trang quản trị - Xử lý phê duyệt/từ chối khiếu nại. Trả về: Thông báo giải quyết khiếu nại.
        [HttpPost("ResolveDispute")]
        public async Task<IActionResult> ResolveDispute([FromQuery] Guid id, [FromBody] ResolveDisputeRequest request)
        {
            await _adminService.ResolveDisputeAsync(id, request);
            return Ok(ApiResponse<string>.SuccessResponse("Dispute resolved successfully. Resolution note published."));
        }

        /// Chức năng: Trang quản trị - Giám sát toàn bộ đơn hàng hệ thống. Trả về: Danh sách đơn hàng phân trang.
        [HttpGet("GetAllOrders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var orders = await _adminService.GetAllOrdersAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<OrderResponse>>.SuccessResponse(orders));
        }

        /// Chức năng: Trang quản trị - Lọc danh sách đơn hàng theo trạng thái. Trả về: Danh sách đơn hàng phân trang.
        [HttpGet("GetOrdersByStatus")]
        public async Task<IActionResult> GetOrdersByStatus([FromQuery] string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var orders = await _adminService.GetOrdersByStatusAsync(status, page, pageSize);
            return Ok(ApiResponse<PagedResult<OrderResponse>>.SuccessResponse(orders));
        }

        /// Chức năng: Trang quản trị - Xem chi tiết đơn hàng hệ thống. Trả về: Thông tin chi tiết toàn diện đơn hàng.
        [HttpGet("GetOrderDetail")]
        public async Task<IActionResult> GetOrderDetailAdmin([FromQuery] Guid orderId)
        {
            var order = await _adminService.GetOrderDetailAsync(orderId);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
        }

        /// Chức năng: Trang quản trị - Quản lý kiểm duyệt sản phẩm sách. Trả về: Danh sách sản phẩm sách phân trang.
        [HttpGet("GetAllBooks")]
        public async Task<IActionResult> GetAllBooks([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var books = await _adminService.GetAllBooksAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<BookResponse>>.SuccessResponse(books));
        }

        /// Chức năng: Trang quản trị - Lọc danh sách sách theo trạng thái. Trả về: Danh sách sách phân trang.
        [HttpGet("GetBooksByStatus")]
        public async Task<IActionResult> GetBooksByStatus([FromQuery] string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var books = await _adminService.GetBooksByStatusAsync(status, page, pageSize);
            return Ok(ApiResponse<PagedResult<BookResponse>>.SuccessResponse(books));
        }

        /// Chức năng: Trang quản trị - Khóa hoặc ẩn sản phẩm sách vi phạm. Trả về: Thông báo ẩn sản phẩm thành công.
        [HttpPut("HideBook")]
        public async Task<IActionResult> HideBook([FromQuery] Guid bookId)
        {
            await _adminService.HideBookAsync(bookId);
            return Ok(ApiResponse<string>.SuccessResponse("Book hidden successfully."));
        }

        /// Chức năng: Trang quản trị - Lấy danh sách đăng ký mở cửa hàng chờ duyệt. Trả về: Danh sách hồ sơ cửa hàng chờ duyệt.
        [HttpGet("GetPendingShops")]
        public async Task<IActionResult> GetPendingShops()
        {
            var shops = await _adminService.GetPendingShopsAsync();
            return Ok(ApiResponse<IEnumerable<ShopResponse>>.SuccessResponse(shops));
        }

        /// Chức năng: Trang quản trị - Quản lý toàn bộ các cửa hàng. Trả về: Danh sách cửa hàng phân trang.
        [HttpGet("GetAllShops")]
        public async Task<IActionResult> GetAllShops([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var shops = await _adminService.GetAllShopsAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<ShopResponse>>.SuccessResponse(shops));
        }

        /// Chức năng: Trang quản trị - Phê duyệt đơn đăng ký mở cửa hàng. Trả về: Thông báo phê duyệt cửa hàng thành công.
        [HttpPost("ApproveShop")]
        public async Task<IActionResult> ApproveShop([FromQuery] Guid shopId)
        {
            await _adminService.ApproveShopAsync(shopId);
            return Ok(ApiResponse<string>.SuccessResponse("Shop approved successfully."));
        }

        /// Chức năng: Trang quản trị - Khóa quyền hoạt động cửa hàng. Trả về: Thông báo khóa cửa hàng thành công.
        [HttpPost("LockShop")]
        public async Task<IActionResult> LockShop([FromQuery] Guid shopId, [FromBody] LockShopRequest request)
        {
            await _adminService.LockShopAsync(shopId, request);
            return Ok(ApiResponse<string>.SuccessResponse("Shop locked successfully."));
        }

        /// Chức năng: Trang quản trị - Thống kê chỉ số hiệu suất toàn sàn. Trả về: Dữ liệu thống kê tổng quan.
        [HttpGet("GetDashboardStatistics")]
        public async Task<IActionResult> GetDashboardStatistics([FromQuery] string period = "month")
        {
            var stats = await _adminService.GetDashboardStatisticsAsync(period);
            return Ok(ApiResponse<DashboardStatisticsResponse>.SuccessResponse(stats));
        }

        /// Chức năng: Trang quản trị - Báo cáo phân tích doanh thu hệ thống. Trả về: Dữ liệu doanh thu theo mốc thời gian.
        [HttpGet("GetRevenueReport")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] string period = "month")
        {
            var revenue = await _adminService.GetRevenueReportAsync(period);
            return Ok(ApiResponse<RevenueReportResponse>.SuccessResponse(revenue));
        }

        /// Chức năng: Trang quản trị - Thống kê các cuốn sách bán chạy nhất. Trả về: Danh sách top sản phẩm sách bán chạy.
        [HttpGet("GetTopSellingBooks")]
        public async Task<IActionResult> GetTopSellingBooks([FromQuery] int limit = 10)
        {
            var books = await _adminService.GetTopSellingBooksAsync(limit);
            return Ok(ApiResponse<IEnumerable<TopSellingBooksResponse>>.SuccessResponse(books));
        }

        /// Chức năng: Trang quản trị / Vận chuyển - Giám sát đơn hàng giao vận. Trả về: Danh sách vận đơn giao hàng phân trang.
        [HttpGet("GetDeliveries")]
        public async Task<IActionResult> GetDeliveries([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var deliveries = await _adminService.GetDeliveriesAsync(status, page, pageSize);
            return Ok(ApiResponse<PagedResult<DeliveryResponse>>.SuccessResponse(deliveries));
        }

        /// Chức năng: Trang quản trị / Vận chuyển - Xem chi tiết vận đơn giao hàng. Trả về: Chi tiết lịch trình giao hàng.
        [HttpGet("GetDeliveryDetail")]
        public async Task<IActionResult> GetDeliveryDetail([FromQuery] Guid deliveryId)
        {
            var delivery = await _adminService.GetDeliveryDetailAsync(deliveryId);
            return Ok(ApiResponse<DeliveryResponse>.SuccessResponse(delivery));
        }

        /// Chức năng: Trang quản trị - Xem toàn bộ danh mục thể loại sách. Trả về: Danh sách toàn bộ thể loại sách.
        [HttpGet("GetAllCategories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(ApiResponse<IEnumerable<CategoryResponse>>.SuccessResponse(categories));
        }

        /// Chức năng: Trang quản trị - Thêm mới thể loại sách. Trả về: Dữ liệu thể loại sách khởi tạo thành công.
        [HttpPost("CreateCategory")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            var category = await _categoryService.CreateCategoryAsync(request);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(category, "Category created successfully."));
        }

        /// Chức năng: Trang quản trị - Cập nhật thông tin thể loại sách. Trả về: Dữ liệu thể loại sách sau cập nhật.
        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategory([FromQuery] Guid id, [FromBody] UpdateCategoryRequest request)
        {
            var category = await _categoryService.UpdateCategoryAsync(id, request);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(category, "Category updated successfully."));
        }

        /// Chức năng: Trang quản trị - Xóa thể loại sách khỏi danh mục. Trả về: Thông báo xóa thể loại sách thành công.
        [HttpDelete("DeleteCategory")]
        public async Task<IActionResult> DeleteCategory([FromQuery] Guid id)
        {
            await _categoryService.DeleteCategoryAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Category deleted successfully."));
        }
    }
}
