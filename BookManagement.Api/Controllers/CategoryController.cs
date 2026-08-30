using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Service.Category;
using BookManagement.Service.Common;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// Chức năng: Lấy danh sách thể loại sách công khai. Trả về: Danh sách thể loại sách đang hoạt động.
        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetActiveCategoriesAsync();
            return Ok(ApiResponse<IEnumerable<CategoryResponse>>.SuccessResponse(categories));
        }

        /// Chức năng: Xem chi tiết thông tin thể loại sách theo ID. Trả về: Dữ liệu chi tiết thể loại sách.
        [HttpGet("GetCategoryDetail")]
        public async Task<IActionResult> GetCategoryDetail(Guid id)
        {
            var category = await _categoryService.GetCategoryAsync(id);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(category));
        }
    }
}
