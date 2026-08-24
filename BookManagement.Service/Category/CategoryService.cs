using BookManagement.Repository.Abstractions;
using BookManagement.Service.Models;
using CategoryEntity = BookManagement.Repository.Entities.Category;

namespace BookManagement.Service.Category;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
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