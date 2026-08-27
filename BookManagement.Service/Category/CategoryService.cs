using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Service.Models;
using Microsoft.EntityFrameworkCore;
using CategoryEntity = BookManagement.Repository.Entities.Category;

namespace BookManagement.Service.Category;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly AppDbContext _context;

    public CategoryService(ICategoryRepository categoryRepository, AppDbContext context)
    {
        _categoryRepository = categoryRepository;
        _context = context;
    }

    public async Task<CategoryResponse> GetCategoryAsync(Guid categoryId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId);

        if (category == null)
            throw new Exception("Category not found");

        return MapToResponse(category);
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return categories.Select(MapToResponse).ToList();
    }

    public async Task<IEnumerable<CategoryResponse>> GetActiveCategoriesAsync()
    {
        var categories = await _categoryRepository.GetActiveAsync();
        return categories.Select(MapToResponse).ToList();
    }

    public async Task<CategoryResponse> CreateCategoryAsync(
        CreateCategoryRequest request)
    {
        if (await _categoryRepository.ExistsByNameAsync(request.Name))
            throw new Exception("Category name already exists");

        var category = new CategoryEntity
        {
            CategoryName = request.Name,
            Description = request.Description,
            Status = true
        };

        await _categoryRepository.AddAsync(category);

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(
        Guid categoryId,
        UpdateCategoryRequest request)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId);

        if (category == null)
            throw new Exception("Category not found");

        if (!string.IsNullOrEmpty(request.Name))
            category.CategoryName = request.Name;

        if (!string.IsNullOrEmpty(request.Description))
            category.Description = request.Description;

        if (request.Status.HasValue)
            category.Status = request.Status.Value;

        await _categoryRepository.UpdateAsync(category);

        return MapToResponse(category);
    }

    public async Task DeleteCategoryAsync(Guid categoryId)
    {
        var hasBooks = await _context.Books.AnyAsync(b => b.CategoryId == categoryId);
        if (hasBooks)
        {
            throw new InvalidOperationException("Không thể xóa danh mục đang có chứa sản phẩm sách. Vui lòng chuyển danh mục hoặc xóa sản phẩm trước.");
        }
        await _categoryRepository.DeleteAsync(categoryId);
    }

    private static CategoryResponse MapToResponse(CategoryEntity category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.CategoryName,
            Description = category.Description,
            Status = category.Status
        };
    }
}