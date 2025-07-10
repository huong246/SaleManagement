using SaleManagement.Schemas;

namespace SaleManagement.Services;

public interface ICategoryService
{
    Task<CategoryDto?> GetCategoryAsync(Guid id);
    Task<List<CategoryDto>> GetAllAsTreeAsync();
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task<bool> DeleteAsync(Guid id);
}