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
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ICategoryService _categoryService;

        public AdminController(IAdminService adminService, ICategoryService categoryService)
        {
            _adminService = adminService;
            _categoryService = categoryService;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] UserFilterRequest filter)
        {
            var users = await _adminService.GetUsersAsync(filter);
            return Ok(ApiResponse<PagedResult<UserResponse>>.SuccessResponse(users));
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {
            var userDetail = await _adminService.GetUserDetailAsync(id);
            return Ok(ApiResponse<UserDetailResponse>.SuccessResponse(userDetail));
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
        {
            await _adminService.UpdateUserStatusAsync(id, request.Status.ToString());
            return Ok(ApiResponse<string>.SuccessResponse($"User status updated to {request.Status}."));
        }

        [HttpGet("disputes")]
        public async Task<IActionResult> GetDisputes([FromQuery] ReturnRequestStatus? status)
        {
            var disputes = await _adminService.GetDisputesAsync(status?.ToString());
            return Ok(ApiResponse<object>.SuccessResponse(disputes));
        }

        [HttpGet("disputes/{id}")]
        public async Task<IActionResult> GetDisputeDetail(Guid id)
        {
            var dispute = await _adminService.GetDisputeDetailAsync(id);
            return Ok(ApiResponse<DisputeResponse>.SuccessResponse(dispute));
        }

        [HttpPost("disputes/{id}/resolve")]
        public async Task<IActionResult> ResolveDispute(Guid id, [FromBody] ResolveDisputeRequest request)
        {
            await _adminService.ResolveDisputeAsync(id, request);
            return Ok(ApiResponse<string>.SuccessResponse("Dispute resolved successfully. Resolution note published."));
        }

        /// <summary>
        /// Admin: Get all orders (monitoring)
        /// </summary>
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var orders = await _adminService.GetAllOrdersAsync(page, pageSize);
            return Ok(ApiResponse<object>.SuccessResponse(orders));
        }

        /// <summary>
        /// Admin: Get orders by status (monitoring)
        /// </summary>
        [HttpGet("orders/status/{status}")]
        public async Task<IActionResult> GetOrdersByStatus(string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var orders = await _adminService.GetOrdersByStatusAsync(status, page, pageSize);
            return Ok(ApiResponse<object>.SuccessResponse(orders));
        }

        /// <summary>
        /// Admin: Get order detail
        /// </summary>
        [HttpGet("orders/{orderId}")]
        public async Task<IActionResult> GetOrderDetailAdmin(Guid orderId)
        {
            var order = await _adminService.GetOrderDetailAsync(orderId);
            return Ok(ApiResponse<object>.SuccessResponse(order));
        }

        /// <summary>
        /// Admin: Get all books (management)
        /// </summary>
        [HttpGet("books")]
        public async Task<IActionResult> GetAllBooks([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var books = await _adminService.GetAllBooksAsync(page, pageSize);
            return Ok(ApiResponse<object>.SuccessResponse(books));
        }

        /// <summary>
        /// Admin: Get books by status (ACTIVE, EMPTY, HIDDEN)
        /// </summary>
        [HttpGet("books/status/{status}")]
        public async Task<IActionResult> GetBooksByStatus(string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var books = await _adminService.GetBooksByStatusAsync(status, page, pageSize);
            return Ok(ApiResponse<object>.SuccessResponse(books));
        }

        /// <summary>
        /// Admin: Hide book
        /// </summary>
        [HttpPut("books/{bookId}/hide")]
        public async Task<IActionResult> HideBook(Guid bookId)
        {
            await _adminService.HideBookAsync(bookId);
            return Ok(ApiResponse<string>.SuccessResponse("Book hidden successfully."));
        }

        /// <summary>
        /// Admin: Get all pending shops (approval)
        /// </summary>
        [HttpGet("shops/pending")]
        public async Task<IActionResult> GetPendingShops()
        {
            var shops = await _adminService.GetPendingShopsAsync();
            return Ok(ApiResponse<object>.SuccessResponse(shops));
        }

        /// <summary>
        /// Admin: Get all shops
        /// </summary>
        [HttpGet("shops")]
        public async Task<IActionResult> GetAllShops([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var shops = await _adminService.GetAllShopsAsync(page, pageSize);
            return Ok(ApiResponse<object>.SuccessResponse(shops));
        }

        /// <summary>
        /// Admin: Approve shop
        /// </summary>
        [HttpPost("shops/{shopId}/approve")]
        public async Task<IActionResult> ApproveShop(Guid shopId)
        {
            await _adminService.ApproveShopAsync(shopId);
            return Ok(ApiResponse<string>.SuccessResponse("Shop approved successfully."));
        }

        /// <summary>
        /// Admin: Lock shop
        /// </summary>
        [HttpPost("shops/{shopId}/lock")]
        public async Task<IActionResult> LockShop(Guid shopId, [FromBody] LockShopRequest request)
        {
            await _adminService.LockShopAsync(shopId, request);
            return Ok(ApiResponse<string>.SuccessResponse("Shop locked successfully."));
        }

        /// <summary>
        /// Admin: Get dashboard statistics
        /// </summary>
        [HttpGet("dashboard/statistics")]
        public async Task<IActionResult> GetDashboardStatistics([FromQuery] string period = "month")
        {
            var stats = await _adminService.GetDashboardStatisticsAsync(period);
            return Ok(ApiResponse<object>.SuccessResponse(stats));
        }

        /// <summary>
        /// Admin: Get revenue report
        /// </summary>
        [HttpGet("dashboard/revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] string period = "month")
        {
            var revenue = await _adminService.GetRevenueReportAsync(period);
            return Ok(ApiResponse<object>.SuccessResponse(revenue));
        }

        /// <summary>
        /// Admin: Get top selling books
        /// </summary>
        [HttpGet("dashboard/top-books")]
        public async Task<IActionResult> GetTopSellingBooks([FromQuery] int limit = 10)
        {
            var books = await _adminService.GetTopSellingBooksAsync(limit);
            return Ok(ApiResponse<object>.SuccessResponse(books));
        }

        /// <summary>
        /// Admin: Get delivery monitoring
        /// </summary>
        [HttpGet("deliveries")]
        public async Task<IActionResult> GetDeliveries([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var deliveries = await _adminService.GetDeliveriesAsync(status, page, pageSize);
            return Ok(ApiResponse<object>.SuccessResponse(deliveries));
        }

        /// <summary>
        /// Admin: Get delivery detail
        /// </summary>
        [HttpGet("deliveries/{deliveryId}")]
        public async Task<IActionResult> GetDeliveryDetail(Guid deliveryId)
        {
            var delivery = await _adminService.GetDeliveryDetailAsync(deliveryId);
            return Ok(ApiResponse<object>.SuccessResponse(delivery));
        }

        /// <summary>
        /// Admin: Get all categories (including inactive)
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(ApiResponse<IEnumerable<CategoryResponse>>.SuccessResponse(categories));
        }

        /// <summary>
        /// Admin: Create category
        /// </summary>
        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            var category = await _categoryService.CreateCategoryAsync(request);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(category, "Category created successfully."));
        }

        /// <summary>
        /// Admin: Update category
        /// </summary>
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
        {
            var category = await _categoryService.UpdateCategoryAsync(id, request);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(category, "Category updated successfully."));
        }

        /// <summary>
        /// Admin: Delete category
        /// </summary>
        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            await _categoryService.DeleteCategoryAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Category deleted successfully."));
        }
    }
}
