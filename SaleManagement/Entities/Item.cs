using System.ComponentModel.DataAnnotations;

namespace SaleManagement.Entities;

public class Item
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int stock { get; set; }
    public Guid ShopId { get; set; }
    public virtual Shop? Shop { get; set; }
    
    public Guid? CategoryId { get; set; }
    public virtual Category? Category { get; set; }
    
    [Timestamp]
    public byte[] RowVersion { get; set; }
}