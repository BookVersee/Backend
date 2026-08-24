using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid categoryId);
    Task<IEnumerable<Category>> GetAllAsync();
    Task<IEnumerable<Category>> GetActiveAsync();
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Guid categoryId);
    Task<bool> ExistsByNameAsync(string name);
}
