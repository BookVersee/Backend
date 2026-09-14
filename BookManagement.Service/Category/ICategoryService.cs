namespace BookManagement.Service.Category;

public interface ICategoryService
{
    Task<CategoryResponse> GetCategoryAsync(Guid categoryId);
    Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync();
    Task<IEnumerable<CategoryResponse>> GetActiveCategoriesAsync();
    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request);
    Task<CategoryResponse> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request);
    Task DeleteCategoryAsync(Guid categoryId);
}
