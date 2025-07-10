using Microsoft.EntityFrameworkCore;
using SaleManagement.Data;
using SaleManagement.Entities;
using SaleManagement.Schemas;

namespace SaleManagement.Services;

public class CategoryService:ICategoryService
{
    private readonly ApiDbContext _dbContext;

    public CategoryService(ApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        var newCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId
        };

        await _dbContext.Categories.AddAsync(newCategory);
        await _dbContext.SaveChangesAsync();

        return new CategoryDto { Id = newCategory.Id, Name = newCategory.Name, Description = newCategory.Description, ParentCategoryId = newCategory.ParentCategoryId };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _dbContext.Categories.FindAsync(id);
        if (category == null)
        {
            return false;
        }

        // Cân nhắc: Thêm logic để xử lý các danh mục con và sản phẩm trước khi xóa
        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync();
        return true;
    }
    

    public async Task<List<CategoryDto>> GetAllAsTreeAsync()
    {
        var allCategories = await _dbContext.Categories.ToListAsync();

        var categoryMap = allCategories.ToDictionary(
            c => c.Id,
            c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, ParentCategoryId = c.ParentCategoryId }
        );

        var tree = new List<CategoryDto>();
        foreach (var category in allCategories)
        {
            if (category.ParentCategoryId.HasValue && categoryMap.ContainsKey(category.ParentCategoryId.Value))
            {
                // Thêm danh mục con vào danh mục cha
                categoryMap[category.ParentCategoryId.Value].SubCategories.Add(categoryMap[category.Id]);
            }
            else
            {
                // Đây là danh mục gốc
                tree.Add(categoryMap[category.Id]);
            }
        }
        return tree;
    }

    public async Task<CategoryDto?>  GetCategoryAsync(Guid id)
    {
        var category = await _dbContext.Categories.FindAsync(id);
        if (category == null)
        {
            return null;
        }
        return new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description };
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await _dbContext.Categories.FindAsync(id);
        if (category == null)
        {
            return false;
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.ParentCategoryId = request.ParentCategoryId;

        await _dbContext.SaveChangesAsync();
        return true;
    }
}