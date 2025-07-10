using SaleManagement.Entities;

namespace SaleManagement.Schemas;

public record CreateCategoryRequest(string Name, string? Description, Guid? ParentCategoryId);
public record UpdateCategoryRequest(Guid CategoryId, string? Name, string? Description, Guid? ParentCategoryId);

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public virtual Category? ParentCategory { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new();
}