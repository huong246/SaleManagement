using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;

[Table("Categories")]
public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public virtual Category? ParentCategory { get; set; }
    
    public virtual ICollection<Category> SubCategories { get; set; } //danh sach cac danh muc con
    public virtual ICollection<Item> Items { get; set; } //danh sach san pham co trong danh muc nay
    public Category()
    {
        SubCategories = new HashSet<Category>();
        Items = new HashSet<Item>();
    }
}