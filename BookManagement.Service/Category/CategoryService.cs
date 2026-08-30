using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Service.Common;
using Microsoft.EntityFrameworkCore;
using CategoryEntity = BookManagement.Repository.Entities.Category;

namespace BookManagement.Service.Category;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryResponse> GetCategoryAsync(Guid categoryId)
    {
        var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null)
            throw new KeyNotFoundException("Category not found");

        return MapToResponse(category);
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync()
    {
        var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();
        return categories.Select(MapToResponse).ToList();
    }

    public async Task<IEnumerable<CategoryResponse>> GetActiveCategoriesAsync()
    {
        var categories = await _context.Categories.AsNoTracking().Where(c => c.Status).OrderBy(c => c.CategoryName).ToListAsync();
        return categories.Select(MapToResponse).ToList();
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var name = request.Name.Trim();
        if (await _context.Categories.AnyAsync(c => c.CategoryName.ToLower() == name.ToLower()))
            throw new InvalidOperationException("Category name already exists");

        var category = new CategoryEntity
        {
            Id = Guid.NewGuid(),
            CategoryName = name,
            Description = request.Description?.Trim(),
            Status = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid categoryId, UpdateCategoryRequest request)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category == null)
            throw new KeyNotFoundException("Category not found");

        if (!string.IsNullOrWhiteSpace(request.Name))
            category.CategoryName = request.Name.Trim();

        if (request.Description != null)
            category.Description = request.Description.Trim();

        if (request.Status.HasValue)
            category.Status = request.Status.Value;

        category.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return MapToResponse(category);
    }

    public async Task DeleteCategoryAsync(Guid categoryId)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category == null) throw new KeyNotFoundException("Category not found.");

        var hasBooks = await _context.Books.AnyAsync(b => b.CategoryId == categoryId);
        if (hasBooks)
        {
            throw new InvalidOperationException("Không thể xóa danh mục đang có chứa sản phẩm sách. Vui lòng chuyển danh mục hoặc xóa sản phẩm trước.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
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