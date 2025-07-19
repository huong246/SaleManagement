using SaleManagement.Schemas;

namespace SaleManagement.Services;



public enum UpdateCategoryResult
{
    Success,
    DatabaseError,
    CategoryNotFound,
    CategoryNotItSelf,
    CategoryNotParent,
}
public interface ICategoryService
{
    Task<CategoryDto?> GetCategoryAsync(Guid id);
    Task<List<CategoryDto>> GetAllAsTreeAsync();
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request);
    Task<UpdateCategoryResult> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task<bool> DeleteAsync(Guid id);
}