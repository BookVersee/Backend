using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Service.Category;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Get active categories for public browsing
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetActiveCategoriesAsync();
            return Ok(ApiResponse<IEnumerable<CategoryResponse>>.SuccessResponse(categories));
        }

        /// <summary>
        /// Get category details by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryDetail(Guid id)
        {
            var category = await _categoryService.GetCategoryAsync(id);
            return Ok(ApiResponse<CategoryResponse>.SuccessResponse(category));
        }
    }
}
